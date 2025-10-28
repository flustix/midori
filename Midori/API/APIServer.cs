using System.Reflection;
using Midori.API.Components;
using Midori.API.Components.Interfaces;
using Midori.Logging;
using Midori.Networking;
using HttpStatusCode = Midori.Networking.HttpStatusCode;

namespace Midori.API;

[Obsolete("Prefer registering API routes with HttpServer.RegisterAPI")]
public class APIServer<T> : IHttpModule
    where T : APIInteraction, new()
{
    private Logger logger { get; } = Logger.GetLogger("API");

    private List<IAPIRoute<T>> routes { get; } = new();

    public bool ShowTimings { get; set; }
    public bool AutoHandleOptions { get; set; } = true;

    public string InternalError { get; set; } = "Welp, something went very wrong. It's probably not your fault, but please report this to the developers.";
    public string NotFoundError { get; set; } = "The requested route does not exist.";

    public void AddRoutesFromAssembly<U>(Assembly assembly)
        where U : IAPIRoute<T>
    {
        assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(U)))
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .ToList()
                .ForEach(t =>
                {
                    var route = (U)Activator.CreateInstance(t)!;
                    routes.Add(route);
                });

        routes.Sort((a, b) => string.Compare(a.RoutePath, b.RoutePath, StringComparison.Ordinal));
    }

    public async Task Process(HttpServerContext ctx)
    {
        try
        {
            var req = ctx.Request;

            IAPIRoute<T>? handler = null;
            Dictionary<string, string> parameters = new();

            if (req.Method == "OPTIONS" && AutoHandleOptions)
            {
                var i = new T();
                i.Populate(ctx, req, new Dictionary<string, string>());
                i.Response.StatusCode = HttpStatusCode.NoContent;
                await i.ReplyData(Array.Empty<byte>());
                return;
            }

            foreach (var r in routes)
            {
                if (!string.Equals(req.Method, r.Method.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var url = req.Target;

                if (r.RoutePath == url && !r.RoutePath.Contains(':'))
                {
                    handler = r; // exact match with no parameters
                    break;
                }

                var parts = r.RoutePath.Split('/');
                var reqParts = url.Split('/');

                if (parts.Length == 0 || reqParts.Length == 0)
                    continue;

                if (reqParts.Last() == "")
                    reqParts = reqParts[..^1]; // remove trailing slash (if any)

                if (parts.Length != reqParts.Length)
                    continue;

                var match = true;
                Dictionary<string, string> reqParams = new();

                for (var i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith(':'))
                    {
                        reqParams.Add(parts[i][1..], reqParts[i]);
                    }
                    else if (!parts[i].Equals(reqParts[i]))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match) continue;

                handler = r;
                parameters = reqParams;
            }

            var interaction = new T();
            interaction.Populate(ctx, req, parameters);

            if (handler == null)
            {
                await interaction.ReplyMessage(HttpStatusCode.NotFound, NotFoundError);
                return;
            }

            try
            {
                var authHandler = interaction as IHasAuthorizationInfo;

                if (handler is INeedsAuthorization)
                {
                    if (authHandler == null)
                        throw new InvalidOperationException("Route requires authorization but interaction does not have authorization info.");

                    if (!authHandler.IsAuthorized)
                    {
                        await interaction.ReplyMessage(HttpStatusCode.Unauthorized, authHandler.AuthorizationError);
                        return;
                    }
                }

                if (ShowTimings)
                    interaction.StartTimer();

                await interaction.HandleRoute(handler);
            }
            catch (Exception ex)
            {
                await interaction.ReplyMessage(HttpStatusCode.InternalServerError, InternalError, ex);
                logger.Add("Error handling route", LogLevel.Error, ex);
            }
        }
        catch (Exception ex)
        {
            logger.Add("Error handling request", LogLevel.Error, ex);
        }
    }
}
