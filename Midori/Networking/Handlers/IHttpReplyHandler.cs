namespace Midori.Networking.Handlers;

public interface IHttpReplyHandler
{
    void Handle(HttpServerContext ctx, HttpStatusCode code, Exception? error);
}
