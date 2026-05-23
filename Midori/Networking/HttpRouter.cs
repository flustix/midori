using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Midori.API;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking.Handlers;
using Midori.Networking.Middleware;

namespace Midori.Networking;

public partial class HttpRouter
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;

    private Dictionary<string, PathMethods> paths { get; } = new();
    private List<Type> middlewares { get; } = new();
    private Dictionary<Type, HttpConnectionManager> managers { get; } = new();

    private Dictionary<string, Type> requestBodyParsers { get; } = new();

    public HttpRouter(ILoggerFactory loggerFactory, IServiceProvider services)
    {
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
        this.services = services;

        RegisterBodyParser<JsonRequestBodyContent>("application/json");
        RegisterBodyParser<MultipartRequestBodyContent>("multipart/form-data");
        RegisterBodyParser<FormUrlEncodedRequestBodyContent>("application/x-www-form-urlencoded");
    }

    #region Body Parsers

    public void RegisterBodyParser<T>(params string[] mimetype)
        where T : IRequestBodyContent
    {
        _ = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance, new[] { typeof(HttpServerContext) })
            ?? throw new InvalidOperationException($"{typeof(T).FullName} does not have a HttpServerContext-only constructor.");

        foreach (var m in mimetype)
            requestBodyParsers[m] = typeof(T);
    }

    public IRequestBodyContent? GetBodyParser(string mime, HttpServerContext ctx)
    {
        foreach (var (m, t) in requestBodyParsers)
        {
            if (!mime.StartsWith(m))
                continue;

            return Activator.CreateInstance(t, ctx) as IRequestBodyContent;
        }

        return null;
    }

    #endregion

    #region Controllers

    public void RegisterControllersFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(x => x.GetCustomAttribute<ControllerAttribute>() != null)
                            .Where(x => !x.IsAbstract).ToList();

        foreach (var type in types)
        {
            var method = typeof(HttpRouter)
                         .GetMethod(nameof(RegisterController), BindingFlags.Instance | BindingFlags.Public)!
                         .MakeGenericMethod(type);

            method.Invoke(this, new object?[] { });
        }
    }

    public void RegisterController<C>()
    {
        assureAPIHandler();

        var type = typeof(C);
        var prefix = string.Empty;

        var ctrlAttr = type.GetCustomAttribute<ControllerAttribute>();
        if (ctrlAttr != null) prefix = ctrlAttr.Prefix;

        if (string.IsNullOrWhiteSpace(prefix))
        {
            logger.LogWarning($"{type.FullName} is missing a {nameof(ControllerAttribute)}. Prefix is defaulting to /.");
            prefix = "/";
        }
        else
            assureValidPrefix(prefix);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                          .Where(x => x.GetCustomAttribute<HttpRouteAttribute>() != null)
                          .ToList();

        if (methods.Count == 0)
            return;

        var modType = typeof(TransientMethodModule<>)
            .MakeGenericType(typeof(ControllerRouteModule<>).MakeGenericType(type));

        var mod = (Activator.CreateInstance(modType) as IMethodModule)!;

        foreach (var method in methods)
        {
            var routeAttr = method.GetCustomAttribute<HttpRouteAttribute>()!;
            var path = Path.Combine(prefix, routeAttr.Path.TrimStart('/')).Replace("\\", "/");
            logger.LogDebug($"Registered {type.FullName}.{method.Name} to {path}");

            addModule(path, routeAttr.Method.GetSystemNet(), mod);
        }
    }

    #endregion

    #region Legacy

    [Obsolete]
    public void RegisterAPI<I, R>(Assembly assembly)
        where I : APIInteraction, new()
        where R : IAPIRoute<I>
    {
        var types = assembly.GetTypes()
                            .Where(t => t.GetInterfaces().Contains(typeof(R)))
                            .Where(t => t is { IsClass: true, IsAbstract: false }).ToList();

        if (types.Count == 0)
        {
            logger.LogWarning($"Could not find any matching routes in assembly '{assembly.FullName}'.");
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
            var method = GetType()
                         .GetMethod(nameof(MapModule), BindingFlags.Instance | BindingFlags.Public)!
                         .MakeGenericMethod(gen);

            method!.Invoke(this, new object?[] { $"{route.RoutePath}", route.Method, null });
        }
    }

    #endregion

    public void AddMiddleware<T>()
        where T : class, IMiddleware
    {
        middlewares.Add(typeof(T));
    }

    internal List<T> GetMiddlewares<T>(IServiceProvider svc)
        where T : IMiddleware
    {
        var matches = middlewares.Where(x => x.IsAssignableTo(typeof(T)));
        return matches.Select(x => (T)ActivatorUtilities.CreateInstance(svc, x)).ToList();
    }

    public HttpConnectionManager<T>? MapModule<T>(string prefix, HttpMethod? method = null, Action<T>? config = null, bool manager = false)
        where T : class, IHttpModule
    {
        var mod = new TransientMethodModule<T> { Configure = config };
        addModule(prefix, method ?? HttpMethod.Get, mod);

        if (manager)
        {
            var m = new HttpConnectionManager<T>();
            managers.Add(typeof(T), m);
            return m;
        }

        return null;
    }

    public IHttpModule? GetModule(string path, HttpMethod method, IServiceScope scope)
    {
        var split = path.Split("/", StringSplitOptions.RemoveEmptyEntries);
        var sorted = paths.OrderByDescending(a => a.Key.Length);

        var (_, methods) = sorted.FirstOrDefault(m =>
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

        var mod = methods?.GetForMethod(method);
        return mod?.CreateHttpModule(scope.ServiceProvider);
    }

    public HttpConnectionManager? GetConnectionManager(Type type) => managers.GetValueOrDefault(type);

    private void addModule(string path, HttpMethod method, IMethodModule mod)
    {
        assureValidPrefix(path);

        if (!paths.ContainsKey(path))
            paths[path] = new PathMethods();

        paths[path].AddMethod(method, mod);
    }

    private void assureAPIHandler()
    {
        var handler = services.GetService<IHttpReplyHandler>();

        if (handler is not IAPIReplyHandler)
            throw new InvalidOperationException($"A reply handler that inherits {nameof(IAPIReplyHandler)} needs to be registered in order for API routes to work.");
    }

    private static void assureValidPrefix(string prefix, string type = "Prefix")
    {
        if (!prefix.StartsWith('/'))
            throw new ArgumentException($"{type} has to start with /.");
        if (prefix.Length > 1 && prefix.EndsWith('/'))
            throw new ArgumentException($"{type} can't end with /.");
    }

    private class PathMethods
    {
        private readonly Dictionary<HttpMethod, IMethodModule> methods = new();

        public void AddMethod(HttpMethod method, IMethodModule mod) => methods.Add(method, mod);
        public IMethodModule? GetForMethod(HttpMethod? method) => method is null ? null : methods.GetValueOrDefault(method);
    }

    private interface IMethodModule
    {
        IHttpModule CreateHttpModule(IServiceProvider services);
    }
}
