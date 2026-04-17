namespace Midori.Networking;

public interface IHttpErrorHandler
{
    void Handle(HttpServerContext context, HttpStatusCode code, Exception? error);
}
