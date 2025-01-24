namespace Midori.Networking.WebSockets.Frame;

public enum WebSocketCloseCode
{
    /// <summary>
    /// Successful operation, connection not required anymore
    /// </summary>
    NormalClosure = 1000,

    /// <summary>
    /// Browser tab closing, graceful server shutdown
    /// </summary>
    GoingAway = 1001,

    /// <summary>
    /// Endpoint received malformed frame
    /// </summary>
    ProtocolError = 1002,

    /// <summary>
    /// Endpoint received unsupported frame (e.g. binary-only got text frame, ping/pong frames not handled properly)
    /// </summary>
    UnsupportedData = 1003,

    /// <summary>
    /// unused
    /// </summary>
    Unused0 = 1004,

    /// <summary>
    /// Got no close status but transport layer finished normally (e.g. TCP FIN but no previous CLOSE frame)
    /// </summary>
    NoStatusRcvd = 1005,

    /// <summary>
    /// Transport layer broke (e.g. couldn't connect, TCP RST)
    /// </summary>
    AbnormalClosure = 1006,

    /// <summary>
    /// Data in endpoint's frame is not consistent (e.g. malformed UTF-8)
    /// </summary>
    InvalidFrameData = 1007,

    /// <summary>
    /// Generic code not applicable to any other (e.g. isn't <see cref="UnsupportedData"/> nor <see cref="MessageTooBig"/>)
    /// </summary>
    PolicyViolation = 1008,

    /// <summary>
    /// Endpoint won't process large message
    /// </summary>
    MessageTooBig = 1009,

    /// <summary>
    /// Client wanted extension(s) that server did not negotiate
    /// </summary>
    MandatoryExtension = 1010,

    /// <summary>
    /// Unexpected server problem while operating
    /// </summary>
    InternalError = 1011,

    /// <summary>
    /// Server/service is restarting
    /// </summary>
    ServiceRestart = 1012,

    /// <summary>
    /// Temporary server condition forced blocking client's application-based request
    /// </summary>
    TryAgainLater = 1013,

    /// <summary>
    /// Server acting as gateway/proxy got invalid response. Equivalent to HTTP 502
    /// </summary>
    BadGateway = 1014,

    /// <summary>
    /// Transport layer broke because TLS handshake failed
    /// </summary>
    TlsHandshakeFailure = 1015
}
