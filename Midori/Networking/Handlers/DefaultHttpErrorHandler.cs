namespace Midori.Networking.Handlers;

public class DefaultHttpErrorHandler : IHttpErrorHandler
{
    public void Handle(HttpServerContext context, HttpStatusCode code, Exception? error)
    {
        var rsp = new HttpResponse(code);
        rsp.WriteToStream(context.Stream).Wait();

        context.Dispose();
        rsp.Dispose();
    }
}
