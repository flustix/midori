using System.Reflection;
using Midori.Logging;
using Midori.Networking.WebSockets.Typed.Proxy;
using Midori.Utils;

namespace Midori.Networking.WebSockets.Typed;

public class TypedWebSocketSession<S, C> : WebSocketSession
    where S : class where C : class
{
    public C Client { get; }
    // public C All { get; }

    internal Dictionary<string, TypedResponseWaitInfo> WaitForResponse = new();

    public TypedWebSocketSession()
    {
        if (!typeof(S).IsInterface || !typeof(C).IsInterface)
            throw new ArgumentException("S and C has to be an interface type.");

        if (!GetType().GetInterfaces().Contains(typeof(S)))
            throw new ArgumentException("Session class has to extend S.");

        Client = TypedImplBuilder<C>.Build(new TypedSingleProxy(this, WaitForResponse));
        // All = TypedImplBuilder<C>.Build(new TypedAllProxy(Array.Empty<WebSocketSession>()));
    }

    internal override async void OnMessageInternal(WebSocketMessage message)
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

            var method = typeof(S).GetMethod(req.MethodName, BindingFlags.Default | BindingFlags.Public | BindingFlags.Instance);

            if (method is null)
                return;

            var mParams = method.GetParameters();
            var args = TypedInvokeRequest.BuildArgsList(mParams.Select(p => p.ParameterType).ToArray(), req.Arguments);

            var isInvoke = method.ReturnType != typeof(Task);

            if (!isInvoke)
            {
                method.Invoke(this, args.ToArray());
                return;
            }

            var ret = await (dynamic)method.Invoke(this, args.ToArray())!;
            var payload = TypedInvokeRequest.Create("", new[] { ret });
            payload.InvokeID = req.InvokeID;
            payload.Type = TypedInvokeRequest.InvokeType.Return;
            await SendAsync(payload.Serialize());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message!", LoggingTarget.Network);
        }
    }
}
