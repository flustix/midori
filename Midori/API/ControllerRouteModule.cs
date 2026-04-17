using Microsoft.Extensions.Logging;
using Midori.Logging;
using Midori.Networking;

namespace Midori.API;

internal class ControllerRouteModule<T> : IHttpModule
    where T : class
{
    private readonly ILogger logger;

    public ControllerRouteModule(ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger(MidoriLoggerProvider.NETWORK);
    }

    public async Task Process(HttpServerContext ctx)
    {
    }
}
