using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Midori.API;
using Midori.API.Components;
using Midori.Logging;

namespace Midori.Networking;

public class HttpServer
{
    private TcpListener listener = null!;
    private Dictionary<string, PathMethods> paths { get; } = new();
    private Dictionary<Type, HttpConnectionManager> managers { get; } = new();

    public IHttpModule? NotFoundModule { get; set; }
    public IHttpModule? MethodNotAllowedModule { get; set; }

    public void Start(IPAddress address, int port)
    {
        listener = new TcpListener(address, port);
        listener.Start();

        var thread = new Thread(receiveLoop) { IsBackground = true };
        thread.Start();
    }

    public void RegisterAPI<I, R>(Assembly assembly)
        where I : APIInteraction, new()
        where R : IAPIRoute<I>
    {
        var types = assembly.GetTypes()
                            .Where(t => t.GetInterfaces().Contains(typeof(R)))
                            .Where(t => t is { IsClass: true, IsAbstract: false }).ToList();

        if (types.Count == 0)
        {
            Logger.Log("Could not find any matching routes in assembly.", LoggingTarget.Network);
            return;
        }

        var routes = types.Select(x =>
        {
            var route = (R)Activator.CreateInstance(x)!;
            assureValidPrefix(route.RoutePath, $"Path of {route.GetType().Name}");
            return route;
        }).ToList();

        routes.Sort((a, b) => string.Compare(a.RoutePath, b.RoutePath, StringComparison.Ordinal));

        foreach (var route in routes)
        {
            var type = route.GetType();
            var gen = typeof(APIRouteModule<,>).MakeGenericType(typeof(I), type);
            var method = typeof(HttpServer)
                         .GetMethod(nameof(MapModule), BindingFlags.Instance | BindingFlags.Public)!
                         .MakeGenericMethod(gen);

            method!.Invoke(this, new object?[] { $"{route.RoutePath}", route.Method, null });
        }
    }

    public HttpConnectionManager<T> MapModule<T>(string prefix, HttpMethod? method = null, Action<T>? config = null)
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
    }

    private static void assureValidPrefix(string prefix, string type = "Prefix")
    {
        if (!prefix.StartsWith('/'))
            throw new ArgumentException($"{type} has to start with /.");
        if (prefix.Length > 1 && prefix.EndsWith('/'))
            throw new ArgumentException($"{type} can't end with /.");
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
        var split = context.Request.Target.Split("?").First().Split("/", StringSplitOptions.RemoveEmptyEntries);
        var sorted = paths.OrderByDescending(a => a.Key.Length);

        var (key, methods) = sorted.FirstOrDefault(m =>
        {
            var ksp = m.Key.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != ksp.Length) return false;

            for (var i = 0; i < split.Length; i++)
            {
                var k = ksp[i];
                var rq = split[i];

                if (k.StartsWith(':'))
                    continue;

                if (!k.Equals(rq, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }

            return true;
        });

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
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(key))
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
                Logger.Log($"No matching module found for {context.Request.Target}!", LoggingTarget.Network, LogLevel.Warning);
            else
                NotFoundModule.Process(context);
        }
    }

    private class PathMethods
    {
        private readonly Dictionary<HttpMethod, RegisteredModule> methods = new();

        public void AddMethod(HttpMethod method, RegisteredModule mod) => methods.Add(method, mod);
        public RegisteredModule? GetForMethod(HttpMethod? method) => method is null ? null : methods.GetValueOrDefault(method);
    }

    private class RegisteredModule
    {
        public Type Type { get; }
        public Action<object>? Config { get; }

        public RegisteredModule(Type type, Action<object>? config)
        {
            Type = type;
            Config = config;
        }
    }
}
