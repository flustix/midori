using System.Reflection;
using Midori.Logging;
using Midori.Networking.WebSockets.Typed.Proxy;
using Midori.Utils;

namespace Midori.Networking.WebSockets.Typed;

public class TypedWebSocketClient<S, C> : ClientWebSocket
    where S : class where C : class
{
    public S Server { get; }
    private C target { get; }

    internal Dictionary<string, TypedResponseWaitInfo> WaitForResponse = new();

    public TypedWebSocketClient(C target)
    {
        if (!typeof(S).IsInterface || !typeof(C).IsInterface)
            throw new ArgumentException("S and C has to be an interface type.");

        OnMessage += onMessage;

        this.target = target;
        Server = TypedImplBuilder<S>.Build(new TypedServerProxy(this, WaitForResponse));
    }

    private async void onMessage(WebSocketMessage message)
    {
        try
        {
            if (!message.IsText)
                return;

            var json = message.Text;
            var req = json.Deserialize<TypedInvokeRequest>()!;

            if (req.Type == TypedInvokeRequest.InvokeType.Return)
            {
                if (!WaitForResponse.Remove(req.InvokeID, out var info))
                    return;

                var ar = TypedInvokeRequest.BuildArgsList(new[] { info.CallbackType }, req.Arguments);
                info.Callback(ar[0]);
                return;
            }

            var method = typeof(C).GetMethod(req.MethodName, BindingFlags.Default | BindingFlags.Public | BindingFlags.Instance);

            if (method is null)
                return;

            var mParams = method.GetParameters();
            var args = TypedInvokeRequest.BuildArgsList(mParams.Select(p => p.ParameterType).ToArray(), req.Arguments);

            var isInvoke = method.ReturnType != typeof(Task);

            if (!isInvoke)
            {
                method.Invoke(target, args.ToArray());
                return;
            }

            var ret = await (dynamic)method.Invoke(target, args.ToArray())!;
            var payload = TypedInvokeRequest.Create("", new[] { ret });
            payload.InvokeID = req.InvokeID;
            payload.Type = TypedInvokeRequest.InvokeType.Return;
            await SendTextAsync(payload.Serialize());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message!", LoggingTarget.Network);
        }
    }
}
