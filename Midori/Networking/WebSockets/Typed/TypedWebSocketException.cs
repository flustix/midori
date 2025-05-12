using JetBrains.Annotations;

namespace Midori.Networking.WebSockets.Typed;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public class TypedWebSocketException : Exception
{
    protected TypedWebSocketException(string message)
        : base(message)
    {
    }
}
