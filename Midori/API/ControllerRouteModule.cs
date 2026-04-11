using System.Reflection;
using Midori.API.Components;
using Midori.Logging;
using Midori.Networking;

namespace Midori.API;

internal class ControllerRouteModule<I, C> : IHttpModule, IControllerRouteModule
    where I : APIInteraction, new()
    where C : new()
{
    public MethodInfo Method { get; set; } = null!;

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
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to handle route {typeof(C).Name}.{Method.Name}.");
        }
    }
}

internal interface IControllerRouteModule
{
    MethodInfo Method { get; set; }
}
