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

            switch (req.Type)
            {
                case TypedInvokeRequest.InvokeType.Return:
                {
                    if (!WaitForResponse.Remove(req.InvokeID, out var info))
                        return;

                    var ar = TypedInvokeRequest.BuildArgsList(new[] { info.CallbackType }, req.Arguments);
                    info.Callback(ar[0]);
                    return;
                }

                case TypedInvokeRequest.InvokeType.Exception:
                {
                    if (!WaitForResponse.Remove(req.InvokeID, out var info))
                        return;

                    if (req.Arguments.Length < 2)
                        return;

                    var typeStr = req.Arguments[0]!.ToObject<string>()!;
                    var exMessage = req.Arguments[1]!.ToObject<string>()!;

                    var type = TypeHelper.FindType(typeStr);

                    if (type is null)
                    {
                        info.ExceptionCallback(new Exception($"{typeStr} {exMessage}"));
                        return;
                    }

                    var exception = (Exception)Activator.CreateInstance(type, exMessage)!;
                    info.ExceptionCallback(exception);
                    return;
                }
            }

            var method = typeof(C).GetMethod(req.MethodName, BindingFlags.Default | BindingFlags.Public | BindingFlags.Instance);

            if (method is null)
                return;

            var mParams = method.GetParameters();
            var args = TypedInvokeRequest.BuildArgsList(mParams.Select(p => p.ParameterType).ToArray(), req.Arguments);

            var isInvoke = method.ReturnType != typeof(Task);

            if (!isInvoke)
            {
                await (Task)method.Invoke(target, args.ToArray())!;
                return;
            }

            dynamic? ret;

            try
            {
                ret = await (dynamic)method.Invoke(target, args.ToArray())!;
            }
            catch (TargetInvocationException inv)
            {
                if (inv.InnerException is not TypedWebSocketException tex)
                    throw;

                await sendException(req, tex);
                return;
            }
            catch (TypedWebSocketException ex)
            {
                await sendException(req, ex);
                return;
            }

            var payload = TypedInvokeRequest.Create("", new[] { ret });
            payload.InvokeID = req.InvokeID;
            payload.Type = TypedInvokeRequest.InvokeType.Return;
            await SendTextAsync(payload.Serialize());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to handle message!", LoggingTarget.Network);
        }

        async Task sendException(TypedInvokeRequest req, Exception ex)
        {
            var error = TypedInvokeRequest.Create("", new object?[] { ex.GetType().FullName, ex.Message });
            error.InvokeID = req.InvokeID;
            error.Type = TypedInvokeRequest.InvokeType.Exception;
            await SendTextAsync(error.Serialize());
        }
    }
}
