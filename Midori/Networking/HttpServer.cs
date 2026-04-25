using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midori.Logging;
using Midori.Networking.Handlers;

namespace Midori.Networking;

public class HttpServer : IHostedService
{
    private TcpListener? listener;
    // private Dictionary<Type, HttpConnectionManager> managers { get; } = new();

    private bool running = true;

    private readonly ILogger logger;
    private readonly HttpRouter router;
    private readonly HttpConfiguration configuration;
    private readonly IHttpReplyHandler replyHandler;

    public HttpServer(HttpRouter router, ILoggerFactory loggerFactory, IOptions<HttpConfiguration> config, IHttpReplyHandler? handler = null)
    {
        this.router = router;
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
        configuration = config.Value;
        replyHandler = handler ?? new DefaultHttpReplyHandler(config);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            listener = new TcpListener(configuration.Address, configuration.Port);
            listener.Start();

            var thread = new Thread(receiveLoop) { IsBackground = true };
            thread.Start();

            logger.LogInformation($"Started listening on {configuration.Address}:{configuration.Port}.");

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        listener?.Dispose();
        running = false;

        logger.LogInformation("Closed HTTP server.");

        return Task.CompletedTask;
    }

    /*public HttpConnectionManager<T> MapModule<T>(string prefix, HttpMethod? method = null, Action<T>? config = null)
        where T : IHttpModule, new()
    {
        assureValidPrefix(prefix);
        method ??= HttpMethod.Get;

        if (!paths.ContainsKey(prefix))
            paths[prefix] = new PathMethods();

        paths[prefix].AddMethod(method, new RegisteredModule(typeof(T), o => config?.Invoke((T)o)));

        if (!managers.ContainsKey(typeof(T)))
            managers.Add(typeof(T), new HttpConnectionManager<T>());

        return (HttpConnectionManager<T>)managers[typeof(T)];
    }*/

    private static void assureValidPrefix(string prefix, string type = "Prefix")
    {
        if (!prefix.StartsWith('/'))
            throw new ArgumentException($"{type} has to start with /.");
        if (prefix.Length > 1 && prefix.EndsWith('/'))
            throw new ArgumentException($"{type} can't end with /.");
    }

    private void receiveLoop()
    {
        while (running && listener != null)
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
        var path = context.Request.Target.Split("?").First();

        HttpMethod? method = context.Request.Method switch
        {
            "GET" => HttpMethod.Get,
            "PUT" => HttpMethod.Put,
            "POST" => HttpMethod.Post,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            "TRACE" => HttpMethod.Trace,
            "PATCH" => HttpMethod.Patch,
            "CONNECT" => HttpMethod.Connect,
            _ => HttpMethod.Get
        };

        IHttpModule? mod;

        try
        {
            mod = router.GetModule(path, method);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to create module for '{path}'!");
            replyHandler.Handle(context, HttpStatusCode.InternalServerError, ex);
            return;
        }

        if (mod is null)
        {
            replyHandler.Handle(context, HttpStatusCode.NotFound, null);
            return;
        }

        try
        {
            mod.Process(context).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to handle module '{mod}' for '{path}'!");
            replyHandler.Handle(context, HttpStatusCode.InternalServerError, ex);
        }

        /*if (!string.IsNullOrWhiteSpace(key))
        {
            HttpConnectionManager? manager = null;
            IHttpModule? module = null;

            try
            {
                var mod = methods.GetForMethod(method);

                if (mod is null)
                {
                    (MethodNotAllowedModule ?? NotFoundModule)?.Process(context);
                    return;
                }

                module = (IHttpModule)Activator.CreateInstance(mod.Type)!;
                mod.Config?.Invoke(module);

                if (managers.TryGetValue(mod.Type, out var m))
                    manager = m;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to create module for path {context.Request.Target}.", LoggingTarget.Network);
            }

            if (module is null)
                return;

            manager?.Add(module);
            module.Process(context).Wait();
            manager?.Remove(module);
        }
        else
        {
            if (NotFoundModule is null)
                Logger.Log($"No matching module found for {context.Request.Target}!", LoggingTarget.Network, LogLevel.Information);
            else
                NotFoundModule.Process(context);
        }*/
    }
}
