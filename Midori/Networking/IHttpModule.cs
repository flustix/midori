namespace Midori.Networking;

public interface IHttpModule
{
    Task Process(HttpServerContext ctx);
}
