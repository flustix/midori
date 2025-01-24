namespace Midori.Networking.WebSockets.Typed.Proxy;

public interface ITypedProxy
{
    Task PerformAsync(string method, object?[] args, CancellationToken token);
}
