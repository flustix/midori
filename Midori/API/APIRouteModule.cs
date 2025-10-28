using Midori.API.Components;
using Midori.API.Components.Interfaces;
using Midori.Logging;
using Midori.Networking;

namespace Midori.API;

public class APIRouteModule<I, R> : IHttpModule
    where I : APIInteraction, new()
    where R : IAPIRoute<I>
{
    public async Task Process(HttpServerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var interaction = new I();

            if (req.Method == "OPTIONS")
            {
                interaction.Populate(ctx, req, new Dictionary<string, string>());
                interaction.Response.StatusCode = HttpStatusCode.NoContent;
                await interaction.ReplyData(Array.Empty<byte>());
                return;
            }

            var route = Activator.CreateInstance<R>();

            var roSplit = route.RoutePath.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var rqSplit = req.Target.Split("/", StringSplitOptions.RemoveEmptyEntries);

            if (roSplit.Length != rqSplit.Length)
            {
                interaction.Populate(ctx, req, new Dictionary<string, string>());
                await interaction.ReplyMessage(HttpStatusCode.NotFound, "The requested route does not exist.");
                return;
            }

            var parameters = new Dictionary<string, string>();

            for (var i = 0; i < roSplit.Length; i++)
            {
                var pRoute = roSplit[i];
                var pRequest = rqSplit[i];

                if (pRoute.StartsWith(':'))
                    parameters.Add(pRoute[1..], pRequest);
            }

            interaction.Populate(ctx, req, parameters);

            try
            {
                var authHandler = interaction as IHasAuthorizationInfo;

                if (route is INeedsAuthorization)
                {
                    if (authHandler == null)
                        throw new InvalidOperationException("Route requires authorization but interaction does not have authorization info.");

                    if (!authHandler.IsAuthorized)
                    {
                        await interaction.ReplyMessage(HttpStatusCode.Unauthorized, authHandler.AuthorizationError);
                        return;
                    }
                }

                await interaction.HandleRoute(route);
            }
            catch (Exception ex)
            {
                await interaction.ReplyMessage(HttpStatusCode.InternalServerError, "Welp, something went very wrong. It's probably not your fault, but please report this to the developers.", ex);
                Logger.Error(ex, "Error handling route", LoggingTarget.Network);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to handle route {typeof(R).Name}.");
        }
    }
}
