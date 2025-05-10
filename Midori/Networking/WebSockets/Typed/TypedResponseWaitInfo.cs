namespace Midori.Networking.WebSockets.Typed;

internal class TypedResponseWaitInfo
{
    public Action<object?> Callback { get; }
    public Action<Exception> ExceptionCallback { get; }
    public Type CallbackType { get; }

    public TypedResponseWaitInfo(Action<object?> callback, Action<Exception> exception, Type callbackType)
    {
        Callback = callback;
        ExceptionCallback = exception;
        CallbackType = callbackType;
    }
}
