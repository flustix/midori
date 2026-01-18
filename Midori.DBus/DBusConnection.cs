using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using Midori.DBus.Attributes;
using Midori.DBus.Exceptions;
using Midori.DBus.Impl;
using Midori.DBus.IO;
using Midori.DBus.Methods;
using Midori.DBus.Values;
using Midori.Logging;
using Midori.Utils;
using Midori.Utils.Extensions;

namespace Midori.DBus;

public class DBusConnection
{
    internal static readonly Logger LOGGER = Logger.GetLogger("DBus");

    public string ClientName { get; private set; } = string.Empty;
    private DBusAddress address { get; }

    private Socket? socket;
    private NetworkStream? stream;
    private bool closed;

    private Dictionary<uint, TaskCompletionSource<DBusMessage>> waiting { get; } = new();

    private readonly TaskCompletionSource<string> helloTask = new();

    public DBusConnection(DBusAddress address)
    {
        this.address = address;
    }

    #region Connect / Close

    public async Task Connect()
    {
        socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var ep = new UnixDomainSocketEndPoint(address.Path);

        LOGGER.Add($"connecting with {ep}", LogLevel.Debug);
        await socket.ConnectAsync(ep);

        stream = new NetworkStream(socket);

        if (!auth())
            throw new AuthenticationException();

        var read = new Thread(startReading) { Name = "DBusStreamReader" };
        read.Start();

        var write = new Thread(writeMessages) { Name = "DBusStreamWriter" };
        write.Start();

        ClientName = await helloTask.Task;
    }

    public async Task Close()
    {
        closed = true;

        if (socket is null) return;
        if (stream is null) return;

        await socket.DisconnectAsync(false);
        socket.Dispose();

        await stream.DisposeAsync();
    }

    #endregion

    #region Authentication

    private bool auth()
    {
        Debug.Assert(stream != null);

        stream.WriteByte(0);

        var user = DBusEnv.UserID;
        var bytes = Encoding.ASCII.GetBytes(user.ToString());
        var hex = string.Join("", bytes.Select(x => x.ToString("x")));
        sendAuthLine($"AUTH EXTERNAL {hex}");

        var res = readAuthLine();
        if (!res.StartsWith("OK")) throw new AuthenticationException(res);

        sendAuthLine("BEGIN");
        return true;
    }

    private void sendAuthLine(string data)
    {
        Debug.Assert(stream != null);

        var bytes = Encoding.ASCII.GetBytes(data + "\r\n");
        stream.Write(bytes, 0, bytes.Length);
    }

    private string readAuthLine()
    {
        Debug.Assert(stream != null);

        var buffer = new List<byte>();

        var end = false;

        while (!end)
        {
            end = stream.ReadByte().EqualTo('\r', add)
                  && stream.ReadByte().EqualTo('\n', add);
        }

        var bytes = buffer.ToArray();
        var str = Encoding.ASCII.GetString(bytes).TrimEnd('\r', '\n');
        return str;

        void add(int v)
        {
            if (v == -1)
                throw new EndOfStreamException("Auth data finished unexpectedly.");

            buffer.Add((byte)v);
        }
    }

    #endregion

    #region Stream Flow

    private uint serial = 1;

    private void startReading()
    {
        Debug.Assert(socket != null);
        Debug.Assert(stream != null);

        Hello().ContinueWith(x =>
        {
            if (x.IsFaulted) helloTask.SetException(x.Exception);
            else helloTask.SetResult(x.Result);
        });

        while (socket.Connected && !closed)
        {
            DBusMessage? message;

            try
            {
                message = DBusMessage.ReadMessage(stream);
            }
            catch (IOException) when (closed)
            {
                return;
            }

            switch (message.Type)
            {
                case DBusMessageType.MethodCall:
                {
                    var path = message.Path;

                    if (!CallPaths.TryGetValue(path, out var handler))
                        CallPaths[path] = handler = new DBusPathHandler(this, path);

                    try
                    {
                        handler.Handle(message);
                    }
                    catch (Exception ex)
                    {
                        QueueMessage(message.CreateError(ex));
                    }

                    break;
                }

                case DBusMessageType.MethodReturn:
                {
                    lock (waiting)
                    {
                        if (waiting.TryGetValue(message.ReplySerial, out var wait))
                        {
                            Task.Run(() => wait.SetResult(message));
                            waiting.Remove(message.ReplySerial);
                        }
                        else
                            LOGGER.Add($"Got reply without matching serial. from:{message.Sender}", LogLevel.Warning);
                    }

                    break;
                }

                case DBusMessageType.Error:
                {
                    lock (waiting)
                    {
                        if (waiting.TryGetValue(message.ReplySerial, out var wait))
                        {
                            var errType = message.ErrorName;
                            var errMsg = message.GetBodyReader().ReadString();

                            wait.SetException(errType switch
                            {
                                "org.freedesktop.DBus.Error.ServiceUnknown" => new DBusServiceUnknownException(errMsg),
                                _ => new DBusException($"{errType}: {errMsg}")
                            });
                        }
                        else
                            LOGGER.Add($"Got error without matching serial. from:{message.Sender}", LogLevel.Warning);
                    }

                    break;
                }

                case DBusMessageType.Signal:
                {
                    var rule = new DBusMatchRule(DBusMatchType.Signal, message.Sender, message.Path, message.Interface, message.Member);
                    List<MatchRuleEntry> matches;

                    lock (matchRules)
                        matches = matchRules.Where(x => x.Matches(rule)).ToList();

                    matches.ForEach(x =>
                    {
                        try
                        {
                            x.Callback.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            LOGGER.Add($"Match '{x.Callback}' caused an exception!", LogLevel.Error, ex);
                            if (Debugger.IsAttached) throw;
                        }
                    });
                    break;
                }

                default:
                    LOGGER.Add($"Server sent '{message.Type}' but we aren't handling this yet!", LogLevel.Warning);
                    break;
            }
        }
    }

    private readonly ConcurrentQueue<DBusMessage> messageQueue = new();

    public void QueueMessage(DBusMessage message)
    {
        Debug.Assert(stream != null);
        messageQueue.Enqueue(message);
    }

    private void writeMessages()
    {
        Debug.Assert(socket != null);
        Debug.Assert(stream != null);

        while (socket.Connected && !closed)
        {
            if (!messageQueue.TryDequeue(out var next))
            {
                Thread.Sleep(10);
                continue;
            }

            next.Write(stream);
        }
    }

    #endregion

    public T CreateProxy<T>(string destination, string path)
        where T : class
    {
        var impl = DBusImplBuilder<T>.Build(this);

        var obj = (impl as DBusObject)!;
        obj.Connection = this;
        obj.Destination = destination;
        obj.Path = path;
        obj.Interface = typeof(T).GetCustomAttributes(true).OfType<DBusInterfaceAttribute>().FirstOrDefault()?.Interface
                        ?? throw new InvalidOperationException($"{typeof(T).FullName} is missing a {nameof(DBusInterfaceAttribute)}.");

        obj.RegisterListeners();
        return impl;
    }

    public async Task<DBusMessage> CallMethod(string dest, string path, string @interface, string member, Action<DBusWriter>? write = null)
    {
        var s = serial++;
        var msg = new DBusMessage(DBusEndian.Little, DBusMessageType.MethodCall, DBusMessageFlags.None, 1, s);
        msg.SetMethodCall(dest, path, @interface, member);
        write?.Invoke(msg.GetBodyWriter());

        var tsc = new TaskCompletionSource<DBusMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (waiting)
            waiting.Add(s, tsc);

        QueueMessage(msg);

        await tsc.Task;
        return tsc.Task.Result;

        /*await Task.WhenAny(tsc.Task, Task.Delay(5000));

        if (!tsc.Task.IsCompleted)
            throw new TimeoutException();

        return tsc.Task.Result;*/
    }

    public async Task<string> Introspect(string dest, string path)
    {
        var msg = await CallMethod(dest, path, "org.freedesktop.DBus.Introspectable", "Introspect");
        var body = msg.GetBodyReader();
        return body.ReadString();
    }

    internal async Task<DBusMessage> CallFromProxy(DBusObject obj, string member, List<object> parameters)
    {
        var objType = obj.GetType();
        var typeParams = objType.GetMethod(member, BindingFlags.Instance | BindingFlags.Public)!.GetParameters();

        var attrs = objType.GetInterfaces().SelectMany(x => x.GetCustomAttributes());
        var intf = attrs.OfType<DBusInterfaceAttribute>().FirstOrDefault()?.Interface;
        if (intf is null) throw new InvalidOperationException($"{objType.Name} is missing a {nameof(DBusInterfaceAttribute)}.");

        return await CallMethod(obj.Destination, obj.Path, intf, member, w =>
        {
            for (var i = 0; i < typeParams.Length; i++)
            {
                w.Write(IDBusValue.GetForType(typeParams[i].ParameterType, parameters[i]));
            }
        });
    }

    internal T GetReturnForProxy<T>(DBusObject obj, string member, DBusMessage message)
    {
        var objType = obj.GetType();
        var method = objType.GetMethod(member, BindingFlags.Instance | BindingFlags.Public)!;
        var ret = method.ReturnType.GetGenericArguments().First();

        var read = message.GetBodyReader();
        var val = read.Read(IDBusValue.GetForType(ret));
        return (T)val;
    }

    internal async Task<DBusMessage> CallDBusMethod(string member, Action<DBusWriter>? write = null) =>
        await CallMethod("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", member, write);

    public async Task<T> GetProperty<T>(string dest, string path, string @interface, string member)
    {
        var msg = await CallMethod(dest, path, "org.freedesktop.DBus.Properties", "Get", w =>
        {
            w.WriteString(@interface);
            w.WriteString(member);
        });

        var read = msg.GetBodyReader();
        var variant = new DBusVariantValue();
        read.Read(variant);
        return (T)variant.Value.Value;
    }

    #region DBus-Methods

    internal async Task<string> Hello()
    {
        var msg = await CallDBusMethod("Hello");
        var body = msg.GetBodyReader();
        return body.ReadString();
    }

    public async Task<uint> RequestName(string name, uint flags = 0)
    {
        var msg = await CallDBusMethod("RequestName", w =>
        {
            w.WriteString(name);
            w.WriteUInt32(flags);
        });

        var read = msg.GetBodyReader();
        return read.ReadUInt32();
    }

    public async Task<uint> ReleaseName(string name)
    {
        var msg = await CallDBusMethod("ReleaseName", w => w.WriteString(name));
        var read = msg.GetBodyReader();
        return read.ReadUInt32();
    }

    public async Task<List<string>> ListQueuedOwners(string name)
    {
        var msg = await CallDBusMethod("ListQueuedOwners", w => w.WriteString(name));
        var read = msg.GetBodyReader();
        return read.ReadArray<DBusStringValue, string>();
    }

    public async Task<List<string>> ListNames()
    {
        var msg = await CallDBusMethod("ListNames");
        var read = msg.GetBodyReader();
        return read.ReadArray<DBusStringValue, string>();
    }

    public async Task<List<string>> ListActivatableNames()
    {
        var msg = await CallDBusMethod("ListActivatableNames");
        var read = msg.GetBodyReader();
        return read.ReadArray<DBusStringValue, string>();
    }

    public async Task<bool> NameHasOwner(string name)
    {
        var msg = await CallDBusMethod("NameHasOwner", w => w.WriteString(name));
        var read = msg.GetBodyReader();
        return read.ReadBool();
    }

    public async Task<string> GetNameOwner(string name)
    {
        var msg = await CallDBusMethod("GetNameOwner", w => w.WriteString(name));
        var read = msg.GetBodyReader();
        return read.ReadString();
    }

    #endregion

    #region Signals

    private List<MatchRuleEntry> matchRules { get; } = new();

    public async Task<InvokeOnDisposal> AddMatch<T>(Action<T> act, DBusMatchRule rule)
    {
        var built = rule.Build();
        var listItem = new MatchRuleEntry(rule, m =>
        {
            var body = m.GetBodyReader();
            var dval = IDBusValue.GetForType(typeof(T));
            body.Read(dval);
            act.Invoke((T)dval.Value);
        });

        lock (matchRules) matchRules.Add(listItem);
        await CallDBusMethod("AddMatch", w => w.WriteString(built));

        return new InvokeOnDisposal(() =>
        {
            lock (matchRules) matchRules.Remove(listItem);
            _ = CallDBusMethod("RemoveMatch", w => w.WriteString(rule.Build()));
        });
    }

    private class MatchRuleEntry
    {
        private DBusMatchRule rule { get; }
        public Action<DBusMessage> Callback { get; }

        public MatchRuleEntry(DBusMatchRule rule, Action<DBusMessage> callback)
        {
            this.rule = rule;
            Callback = callback;
        }

        public bool Matches(DBusMatchRule r) => rule.Equals(r);
    }

    #endregion

    #region Callable Interfaces

    internal Dictionary<DBusObjectPath, IDBusPathHandler> CallPaths { get; } = new();

    public void RegisterPathHandler(IDBusPathHandler inst, DBusObjectPath path)
    {
        if (CallPaths.TryGetValue(path, out _))
            throw new InvalidOperationException($"Handler with path {path} is already registered.");

        CallPaths[path] = inst;
    }

    public void RegisterInterface(IDBusInterfaceHandler inst, DBusObjectPath path)
    {
        if (!CallPaths.TryGetValue(path, out var handle))
            CallPaths[path] = handle = new DBusPathHandler(this, path);

        handle.RegisterInterface(inst);
    }

    public void RegisterInterface<T>(T inst, DBusObjectPath path)
        where T : class
    {
        if (!CallPaths.TryGetValue(path, out var handle))
            CallPaths[path] = handle = new DBusPathHandler(this, path);

        handle.RegisterInterface(inst);
    }

    #endregion
}
