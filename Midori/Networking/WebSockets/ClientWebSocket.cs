using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using Midori.Logging;

namespace Midori.Networking.WebSockets;

public class ClientWebSocket : WebSocket
{
    protected override bool MaskData => true;

    public HttpHeaderCollection RequestHeaders { get; } = new();
    public uint PingInterval { get; init; }

    private TcpClient client = null!;
    private Uri uri = null!;
    private bool secure = false;

    public async Task ConnectAsync(string uri) => await Task.Run(() => Connect(uri));

    public void Connect(string uri)
    {
        this.uri = new Uri(uri);
        var scheme = this.uri.Scheme;

        if (scheme is not ("ws" or "wss"))
            throw new ArgumentException("URI scheme has to be 'ws' or 'wss'.");

        secure = this.uri.Scheme == "wss";

        var success = connect();

        if (!success)
            return;

        var thread = new Thread(Open) { Name = $"ClientWebSocket({this.uri})" };
        thread.Start();

        if (PingInterval > 0)
        {
            var ping = new Thread(() =>
            {
                while (State == WebSocketState.Open)
                {
                    Thread.Sleep((int)PingInterval);
                    Ping();
                }
            }) { Name = "WebSocket ping thread" };
            ping.Start();
        }
    }

    private bool connect()
    {
        if (State == WebSocketState.Connecting)
            return false;

        State = WebSocketState.Connecting;

        doHandshake();

        State = WebSocketState.Open;

        return true;
    }

    private void doHandshake()
    {
        createStream();

        var handshake = sendHandshake();
        handshake.Wait();

        var res = handshake.Result;

        if (res.StatusCode != HttpStatusCode.SwitchingProtocols)
        {
            var message = new StreamReader(res.BodyStream).ReadToEnd();
            throw new InvalidOperationException($"{res.StatusCode} {message}");
        }
    }

    private void createStream()
    {
        client = new TcpClient(uri.DnsSafeHost, uri.Port);
        Stream = client.GetStream();

        if (secure)
        {
            try
            {
                // I cannot be bothered with proper ssl
                var ssl = new SslStream(Stream, false, (_, _, _, _) => true, (_, _, _, _, _) => null!);
                ssl.AuthenticateAsClient(uri.DnsSafeHost, null, SslProtocols.None, false);
                Stream = ssl;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to create secure stream.", LoggingTarget.Network);
                throw;
            }
        }
    }

    private async Task<HttpResponse> sendHandshake()
    {
        var req = new HttpRequest("GET", uri.PathAndQuery, "1.1", RequestHeaders);
        req.Headers["Host"] = uri.DnsSafeHost;
        req.Headers["Upgrade"] = "websocket";
        req.Headers["Connection"] = "Upgrade";
        req.Headers["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==";
        req.Headers["Sec-WebSocket-Version"] = "13";
        await req.WriteToStream(Stream);
        return HttpResponse.ReadResponse(Stream);
    }

    public override void Dispose()
    {
        base.Dispose();
        client.Close();
    }

    public override string ToString() => $"{uri} {base.ToString()}";
}
