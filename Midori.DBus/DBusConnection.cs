using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using Midori.DBus.Attributes;
using Midori.DBus.Exceptions;
using Midori.DBus.Impl;
using Midori.DBus.IO;
using Midori.DBus.Values;
using Midori.Logging;
using Midori.Utils.Extensions;

namespace Midori.DBus;

public class DBusConnection
{
    private readonly Logger logger = Logger.GetLogger("DBus");

    private DBusAddress address { get; }

    private Socket? socket;
    private NetworkStream? stream;
    private bool closed;

    private Dictionary<uint, TaskCompletionSource<DBusMessage>> waiting { get; } = new();

    private readonly TaskCompletionSource helloTask = new();

    public DBusConnection(DBusAddress address)
    {
        this.address = address;
    }

    #region Connect / Close

    public async Task Connect()
    {
        socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var ep = new UnixDomainSocketEndPoint(address.Path);

        logger.Add($"connecting with {ep}", LogLevel.Debug);
        await socket.ConnectAsync(ep);

        stream = new NetworkStream(socket);

        if (!auth())
            throw new AuthenticationException();

        var thread = new Thread(startReading) { Name = "DBusStreamReader" };
        thread.Start();

        await helloTask.Task;
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
            else helloTask.SetResult();
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
                case DBusMessageType.MethodReturn:
                {
                    var se = (message.Headers[DBusHeaderID.ReplySerial] as DBusUInt32Value)!.Value;

                    lock (waiting)
                    {
                        if (waiting.TryGetValue(se, out var wait))
                            wait.SetResult(message);
                    }

                    break;
                }

                case DBusMessageType.Error:
                {
                    var se = (message.Headers[DBusHeaderID.ReplySerial] as DBusUInt32Value)!.Value;
                    var errType = (message.Headers[DBusHeaderID.ErrorName] as DBusStringValue)!.Value;
                    var errMsg = message.GetBodyReader().ReadString();

                    lock (waiting)
                    {
                        if (waiting.TryGetValue(se, out var wait))
                        {
                            wait.SetException(errType switch
                            {
                                "org.freedesktop.DBus.Error.ServiceUnknown" => new DBusServiceUnknownException(errMsg),
                                _ => new DBusException($"{errType}: {errMsg}")
                            });
                        }
                    }

                    break;
                }
            }
        }
    }

    public void SendMessage(DBusMessage message)
    {
        Debug.Assert(stream != null);
        message.Write(stream);
    }

    #endregion

    public T CreateProxy<T>(string destination, string path)
        where T : class
    {
        var impl = DBusImplBuilder<T>.Build(this);

        var obj = (impl as DBusObject)!;
        obj.Destination = destination;
        obj.Path = path;

        return impl;
    }

    public async Task<DBusMessage> CallMethod(string dest, string path, string @interface, string member, Action<DBusWriter>? write = null)
    {
        var s = serial++;

        var msg = new DBusMessage(DBusEndian.Little, DBusMessageType.MethodCall, 0, 1, s);
        msg.SetMethodCall(dest, path, @interface, member);
        write?.Invoke(msg.GetBodyWriter());

        var tsc = new TaskCompletionSource<DBusMessage>();

        lock (waiting)
            waiting.Add(s, tsc);

        SendMessage(msg);

        var timeout = Task.Delay(20000);
        await Task.WhenAny(tsc.Task, timeout);

        if (!tsc.Task.IsCompleted)
            throw new TimeoutException();

        return tsc.Task.Result;
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
                var type = typeParams[i].ParameterType;
                var val = parameters[i];

                switch (type.Name)
                {
                    case nameof(String):
                        w.WriteString((string)val);
                        break;

                    case nameof(UInt32):
                        w.WriteUInt32((uint)val);
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid parameter type {type}.");
                }
            }
        });
    }

    internal async Task<DBusMessage> CallDBusMethod(string member, Action<DBusWriter>? write = null) =>
        await CallMethod("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", member, write);

    #region DBus-Methods

    internal async Task<string> Hello()
    {
        var msg = await CallDBusMethod("Hello");
        var body = msg.GetBodyReader();
        return body.ReadString();
    }

    public async Task<uint> RequestName(string name, uint flags)
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

    #endregion
}
