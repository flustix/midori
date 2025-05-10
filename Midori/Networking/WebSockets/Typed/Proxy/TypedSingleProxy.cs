using Midori.Utils;

namespace Midori.Networking.WebSockets.Typed.Proxy;

public class TypedSingleProxy : ITypedProxy, ITypedSingleProxy
{
    private Dictionary<string, TypedResponseWaitInfo> waitInfos;
    private WebSocketSession session { get; }

    internal TypedSingleProxy(WebSocketSession session, Dictionary<string, TypedResponseWaitInfo> waitInfos)
    {
        this.session = session;
        this.waitInfos = waitInfos;
    }

    public async Task PerformAsync(string method, object?[] args, CancellationToken token)
    {
        var request = TypedInvokeRequest.Create(method, args);
        await session.SendAsync(request.Serialize());
    }

    public async Task<T> PerformWithReturnAsync<T>(string method, object?[] args, CancellationToken token)
    {
        var request = TypedInvokeRequest.Create(method, args);

        var tsc = new TaskCompletionSource<T>();
        waitInfos.Add(request.InvokeID, new TypedResponseWaitInfo(res => tsc.SetResult((T)res!), tsc.SetException, typeof(T)));

        await session.SendAsync(request.Serialize());
        await Task.WhenAny(tsc.Task, Task.Delay(5000, token));

        if (!tsc.Task.IsCompleted)
            throw new TimeoutException("The request timed out!");

        return await tsc.Task;
    }
}
