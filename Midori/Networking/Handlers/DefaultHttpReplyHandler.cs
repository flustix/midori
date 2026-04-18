namespace Midori.Networking.Handlers;

public class DefaultHttpReplyHandler : IHttpReplyHandler
{
    public void Handle(HttpServerContext ctx, HttpStatusCode code, Exception? error)
    {
        var rsp = new HttpResponse(code);
        rsp.WriteToStream(ctx.Stream).Wait();

        ctx.Dispose();
        rsp.Dispose();
    }
}
