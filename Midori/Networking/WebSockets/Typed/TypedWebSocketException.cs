namespace Midori.Networking.WebSockets.Typed;

public class TypedWebSocketException : Exception
{
    protected TypedWebSocketException(string message)
        : base(message)
    {
    }
}
