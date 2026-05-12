using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking.Handlers;

namespace Midori.Networking;

public class HttpServer : IHostedService
{
    private TcpListener? listener;

    private bool running = true;

    private readonly ILogger logger;
    private readonly HttpRouter router;
    private readonly HttpConfiguration configuration;
    private readonly IServiceProvider services;

    public HttpServer(HttpRouter router, ILoggerFactory loggerFactory, IOptions<HttpConfiguration> config, IServiceProvider services)
    {
        this.router = router;
        this.services = services;
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
        configuration = config.Value;
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
                    HttpServerContext? ctx = null;

                    try
                    {
                        ctx = new HttpServerContext(client);
                        processClient(ctx);
                    }
                    finally
                    {
                        ctx?.Dispose();
                        client?.Dispose();
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
        var scope = services.CreateScope();
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

        var handler = scope.ServiceProvider.GetService<IHttpReplyHandler>() ?? new DefaultAPIReplyHandler(new OptionsWrapper<HttpConfiguration>(configuration));

        if (method == HttpMethod.Options && configuration.AutoHandleOptions)
        {
            handler.Handle(context, HttpStatusCode.OK, null);
            return;
        }

        IHttpModule? mod;

        try
        {
            mod = router.GetModule(path, method, scope);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to create module for '{path}'!");
            handler.Handle(context, HttpStatusCode.InternalServerError, ex);
            return;
        }

        if (mod is null)
        {
            handler.Handle(context, HttpStatusCode.NotFound, null);
            return;
        }

        var manager = router.GetConnectionManager(mod.GetType());
        manager?.Add(mod);

        try
        {
            mod.Process(context).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to handle module '{mod}' for '{path}'!");
            handler.Handle(context, HttpStatusCode.InternalServerError, ex);
        }

        manager?.Remove(mod);
    }
}
