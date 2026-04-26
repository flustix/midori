using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking;
using Midori.Networking.Handlers;
using Midori.Utils.Extensions;

namespace Midori.API;

internal partial class ControllerRouteModule<T> : IHttpModule
    where T : class
{
    private readonly ILogger logger;
    private readonly IAPIAuthenticator? defaultAuth;
    private readonly IServiceProvider services;
    private readonly IAPIReplyHandler replyHandler;

    public ControllerRouteModule(ILoggerFactory loggerFactory, IServiceProvider services, IHttpReplyHandler replyHandler, IAPIAuthenticator? defaultAuth = null)
    {
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
        this.defaultAuth = defaultAuth;
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
                    rawParams[k[1..]] = rq;
                    continue;
                }

                if (!k.Equals(rq, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }

            return true;
        });

        if (method is null)
            throw new InvalidOperationException($"Failed to get method for path '{ctx.Request.Target}'.");

        var authenticated = method.GetCustomAttribute<AuthenticatedAttribute>();
        var authData = new Dictionary<string, object>();

        if (authenticated != null)
        {
            IAPIAuthenticator? auth;

            if (authenticated.Authenticator != null)
            {
                auth = ActivatorUtilities.CreateInstance(services, authenticated.Authenticator) as IAPIAuthenticator
                       ?? throw new InvalidOperationException($"{authenticated.Authenticator} is not an {nameof(IAPIAuthenticator)}.");
            }
            else
                auth = defaultAuth;

            if (auth is null)
            {
                if (authenticated.Required)
                    throw new InvalidOperationException($"'{typeof(T)}.{method.Name}' requires authentication but no {nameof(IAPIAuthenticator)} is registered.");
            }
            else
            {
                var success = auth.Authenticate(ctx, out var userScopes, out authData);

                if (!success && authenticated.Required)
                {
                    replyHandler.Handle<object>(ctx, Returns.Message(HttpStatusCode.Unauthorized, "Invalid authentication data."));
                    return;
                }

                if (success && authenticated.Scopes.Length > 0 && !userScopes.Contains("*"))
                {
                    foreach (var scope in authenticated.Scopes)
                    {
                        if (userScopes.Contains(scope))
                            continue;

                        replyHandler.Handle<object>(ctx, Returns.Message(HttpStatusCode.Forbidden, $"Missing scope '{scope}'."));
                        return;
                    }
                }
            }
        }

        var instance = ActivatorUtilities.CreateInstance<T>(services);

        List<object?> parameters;

        try
        {
            parameters = getCallParameters(ctx, method, rawParams, authData);
        }
        catch (KeyNotFoundException ex)
        {
            replyHandler.Handle<object>(ctx, Returns.Message(HttpStatusCode.BadRequest, ex.Message));
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get parameters!");
            return;
        }

        var result = method.Invoke(instance, parameters.ToArray())!;
        var resultType = result.GetType();

        if (!resultType.IsGenericType && resultType.GetGenericTypeDefinition() != typeof(APIReturn<>))
            throw new InvalidOperationException($"{typeof(T)}.{method.Name} does not return APIReturn<>.");

        var gen = resultType.GetGenericArguments().First();
        var handle = replyHandler.GetType().GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.MakeGenericMethod(gen);
        handle.Invoke(replyHandler, new[] { ctx, result });
    }

    private List<object?> getCallParameters(HttpServerContext ctx, MethodInfo method, Dictionary<string, string> pathParameters, Dictionary<string, object> authData)
    {
        var result = new List<object?>();

        foreach (var parameter in method.GetParameters())
        {
            var type = parameter.ParameterType;
            var srcAttr = parameter.GetCustomAttribute<SourceAttribute>();
            var source = srcAttr?.Source ?? ParameterSource.Path;
            var name = /*srcAttr?.Name ?? */parameter.Name ?? throw new InvalidOperationException($"Parameter does not have a name [{type}].");
            var optional = parameter.IsNullable() || parameter.HasDefaultValue;

            object? value = null;

            if (authData.TryGetValue(name, out var auth))
                value = auth;
            else if (type == typeof(HttpServerContext))
                value = ctx;
            else if (source == ParameterSource.Body)
            {
                value = getBody(ctx).GetFull(type);

                if (value is null && !optional)
                    throw new KeyNotFoundException("Missing request body.");

                if (optional)
                    value = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }
            else if (source == ParameterSource.Form)
            {
                value = getBody(ctx).GetFormEntry(type, name);
                if (value is null && optional) value = parameter.HasDefaultValue ? parameter.DefaultValue : null;
                else if (value is null) throw getMissingParam(name, source);
            }
            else
            {
                string? raw = source switch
                {
                    ParameterSource.Path => pathParameters.GetValueOrDefault(name),
                    ParameterSource.Query => ctx.Request.QueryParameters.GetValueOrDefault(name),
                    ParameterSource.Headers => ctx.Request.Headers.Get(name),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (string.IsNullOrEmpty(raw))
                {
                    if (!optional)
                        throw getMissingParam(name, source);

                    result.Add(parameter.HasDefaultValue ? parameter.DefaultValue : null);
                    continue;
                }

                if (type == typeof(string))
                    value = raw;
                else if (type == typeof(int))
                    value = raw.TryParseIntInvariant(out var v) ? v : throw getInvalidType(name, "int");
                else if (type == typeof(long))
                    value = raw.TryParseLongInvariant(out var v) ? v : throw getInvalidType(name, "long");
                else if (type == typeof(float))
                    value = raw.TryParseFloatInvariant(out var v) ? v : throw getInvalidType(name, "float");
                else if (type == typeof(double))
                    value = raw.TryParseDoubleInvariant(out var v) ? v : throw getInvalidType(name, "double");
            }

            if (value is null)
                throw new InvalidOperationException($"Type {type} is not supported as parameter.");

            result.Add(value);
        }

        return result;

        Exception getMissingParam(string name, ParameterSource source)
            => throw new KeyNotFoundException($"Parameter '{name}' is missing in {source.ToString().ToLower()}.");

        Exception getInvalidType(string name, string type)
            => new KeyNotFoundException($"Parameter '{name}' is not a valid '{type}'.");
    }
}
