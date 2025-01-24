using Midori.Networking.WebSockets.Frame;

namespace Midori.Utils.Extensions;

public static class WebSocketExtensions
{
    public static bool IsReservedCode(this WebSocketCloseCode code) => code is WebSocketCloseCode.Unused0
        or WebSocketCloseCode.NoStatusRcvd
        or WebSocketCloseCode.AbnormalClosure
        or WebSocketCloseCode.TlsHandshakeFailure;
}
