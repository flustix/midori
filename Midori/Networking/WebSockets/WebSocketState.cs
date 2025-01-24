namespace Midori.Networking.WebSockets;

public enum WebSocketState : byte
{
    /// <summary>
    /// Nothing has happened yet.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// The connection is being established. (Handshake)
    /// </summary>
    Connecting = 0x1,

    /// <summary>
    /// The connection is open and ready to send/receive data.
    /// </summary>
    Open = 0x2,

    /// <summary>
    /// The connection is being closed.
    /// </summary>
    Closing = 0x3,

    /// <summary>
    /// The connection has been closed gracefully.
    /// </summary>
    Closed = 0x4,

    /// <summary>
    /// The connection has been aborted due to an error.
    /// </summary>
    Aborted = 0x5
}
