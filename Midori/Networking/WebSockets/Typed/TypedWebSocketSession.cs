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
        string? json = null;

        try
        {
            if (!message.IsText)
                return;

            json = message.Text;
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
                    if (req.Arguments.Length < 2)
                        return;

                    if (!WaitForResponse.Remove(req.InvokeID, out var info))
                        return;

                    var typeStr = req.Arguments[0]!.ToObject<string>()!;
                    var exMessage = req.Arguments[1]!.ToObject<string>()!;

                    var type = Type.GetType(typeStr);

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

            var method = typeof(S).GetMethod(req.MethodName, BindingFlags.Default | BindingFlags.Public | BindingFlags.Instance);

            if (method is null)
                return;

            var mParams = method.GetParameters();
            var args = TypedInvokeRequest.BuildArgsList(mParams.Select(p => p.ParameterType).ToArray(), req.Arguments);

            var isInvoke = method.ReturnType != typeof(Task);

            if (!isInvoke)
            {
                await (Task)method.Invoke(this, args.ToArray())!;
                return;
            }

            dynamic? ret;

            try
            {
                ret = await (dynamic)method.Invoke(this, args.ToArray())!;
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
            await SendAsync(payload.Serialize());
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to handle message! {json}", LoggingTarget.Network);
        }

        async Task sendException(TypedInvokeRequest req, Exception ex)
        {
            var error = TypedInvokeRequest.Create("", new object?[] { ex.GetType().FullName, ex.Message });
            error.InvokeID = req.InvokeID;
            error.Type = TypedInvokeRequest.InvokeType.Exception;
            await SendAsync(error.Serialize());
        }
    }
}
