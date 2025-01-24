using Midori.Utils;

namespace Midori.Networking.WebSockets.Typed.Proxy;

internal class TypedAllProxy : ITypedProxy
{
    private WebSocketSession[] sessions { get; }

    public TypedAllProxy(WebSocketSession[] sessions)
    {
        this.sessions = sessions;
    }

    public async Task PerformAsync(string method, object?[] args, CancellationToken token)
    {
        var request = TypedInvokeRequest.Create(method, args);
        var json = request.Serialize();

        foreach (var session in sessions)
            await session.SendAsync(json);
    }
}
