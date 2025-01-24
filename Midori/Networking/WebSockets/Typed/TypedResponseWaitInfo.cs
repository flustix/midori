namespace Midori.Networking.WebSockets.Typed;

internal class TypedResponseWaitInfo
{
    public Action<object?> Callback { get; }
    public Type CallbackType { get; }

    public TypedResponseWaitInfo(Action<object?> callback, Type callbackType)
    {
        Callback = callback;
        CallbackType = callbackType;
    }
}
