using System.Net;
using System.Net.Sockets;
using Midori.Logging;

namespace Midori.Networking;

public class HttpServer
{
    private TcpListener listener = null!;
    private Dictionary<string, (Type, Action<object>?)> modules { get; } = new();
    private Dictionary<Type, HttpConnectionManager> managers { get; } = new();

    public void Start(IPAddress address, int port)
    {
        listener = new TcpListener(address, port);
        listener.Start();

        var thread = new Thread(receiveLoop) { IsBackground = true };
        thread.Start();
    }

    public HttpConnectionManager<T> MapModule<T>(string prefix, Action<T>? config = null)
        where T : IHttpModule, new()
    {
        if (!prefix.StartsWith('/'))
            throw new ArgumentException("Prefix has to start with /.");

        modules.Add(prefix, (typeof(T), o => config?.Invoke((T)o)));

        if (!managers.ContainsKey(typeof(T)))
            managers.Add(typeof(T), new HttpConnectionManager<T>());

        return (HttpConnectionManager<T>)managers[typeof(T)];
    }

    private void receiveLoop()
    {
        while (true)
        {
            TcpClient? client = null;

            try
            {
                client = listener.AcceptTcpClient();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var ctx = new HttpServerContext(client);
                        processClient(ctx);
                    }
                    catch (Exception)
                    {
                        client.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to accept client!", LoggingTarget.Network);
                client?.Close();
                break;
            }
        }
    }

    private void processClient(HttpServerContext context)
    {
        var sorted = modules.OrderByDescending(a => a.Key.Length);
        var (key, _) = sorted.FirstOrDefault(m => context.Request.Target.StartsWith(m.Key));

        if (!string.IsNullOrWhiteSpace(key))
        {
            HttpConnectionManager? manager = null;
            IHttpModule? module = null;

            try
            {
                var (type, config) = modules[key];
                module = (IHttpModule)Activator.CreateInstance(type)!;
                config?.Invoke(module);
                manager = managers[type];
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to create module for path {context.Request.Target}.", LoggingTarget.Network);
            }

            if (module is null || manager is null)
                return;

            manager.Add(module);
            module.Process(context).Wait();
            manager.Remove(module);
        }
        else
            Logger.Log($"No matching module found for {context.Request.Target}!", LoggingTarget.Network, LogLevel.Warning);
    }
}
