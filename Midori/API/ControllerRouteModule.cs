using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Midori.API.Components;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking;
using Midori.Networking.Handlers;
using Midori.Utils.Extensions;

namespace Midori.API;

internal class ControllerRouteModule<T> : IHttpModule
    where T : class
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;
    private readonly IAPIReplyHandler replyHandler;

    public ControllerRouteModule(ILoggerFactory loggerFactory, IServiceProvider services, IHttpReplyHandler replyHandler)
    {
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
        this.services = services;
        this.replyHandler = (replyHandler as IAPIReplyHandler)!;
    }

    public async Task Process(HttpServerContext ctx)
    {
        var split = ctx.Request.Target.Split("/", StringSplitOptions.RemoveEmptyEntries);

        var methods = typeof(T).GetControllerMethods()
                               .OrderByDescending(x => x.path.Length)
                               .ToList();

        var rawParams = new Dictionary<string, string>();

        var (method, _, _) = methods.FirstOrDefault(x =>
        {
            var (_, _, p) = x;

            var ksp = p.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != ksp.Length) return false;

            for (var i = 0; i < split.Length; i++)
            {
                var k = ksp[i];
                var rq = split[i];

                if (k.StartsWith(':'))
                {
                    rawParams.Add(k[1..], rq);
                    continue;
                }

                if (!k.Equals(rq, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }

            return true;
        });

        if (method is null)
            throw new InvalidOperationException($"Failed to get method for path '{ctx.Request.Target}'.");

        var instance = ActivatorUtilities.CreateInstance<T>(services);

        var result = method.Invoke(instance, Array.Empty<object>())!;
        var resultType = result.GetType();

        if (!resultType.IsGenericType && resultType.GetGenericTypeDefinition() != typeof(APIReturn<>))
            throw new InvalidOperationException($"{typeof(T)}.{method.Name} does not return APIReturn<>.");

        var gen = resultType.GetGenericArguments().First();
        var handle = replyHandler.GetType().GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.MakeGenericMethod(gen);
        handle.Invoke(replyHandler, new[] { ctx, result });
    }
}
