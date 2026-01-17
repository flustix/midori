namespace Midori.Utils;

public class InvokeOnDisposal : IDisposable
{
    private readonly Action action;

    public InvokeOnDisposal(Action action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        action.Invoke();
    }
}

public class InvokeOnDisposal<T> : IDisposable
{
    private readonly T inst;
    private readonly Action<T> action;

    public InvokeOnDisposal(T inst, Action<T> action)
    {
        this.inst = inst ?? throw new ArgumentNullException(nameof(inst));
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose()
    {
        action.Invoke(inst);
    }
}
