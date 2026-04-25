using Microsoft.Extensions.Options;

namespace Midori.Networking.Handlers;

public class DefaultHttpReplyHandler : IHttpReplyHandler
{
    private readonly HttpConfiguration config;

    public DefaultHttpReplyHandler(IOptions<HttpConfiguration> config)
    {
        this.config = config.Value;
    }

    public void Handle(HttpServerContext ctx, HttpStatusCode code, Exception? error)
    {
        var rsp = new HttpResponse(code);
        config.ApplyHeaders(rsp.Headers);
        rsp.WriteToStream(ctx.Stream).Wait();

        ctx.Dispose();
        rsp.Dispose();
    }
}
