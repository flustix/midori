namespace Midori.Networking.WebSockets.Frame;

public enum WebSocketOpcode : byte
{
    Continuation = 0x0, // 0
    Text = 0x1, // 1
    Binary = 0x2, // 2
    Close = 0x8, // 8
    Ping = 0x9, // 9
    Pong = 0xA // 10
}
