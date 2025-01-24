namespace Midori.Networking.WebSockets.Typed.Proxy;

public interface ITypedSingleProxy
{
    Task<T> PerformWithReturnAsync<T>(string method, object?[] args, CancellationToken token);
}
