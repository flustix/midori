using System.Net;
using System.Security.Cryptography;
using System.Text;
using Midori.Utils;

namespace Midori.Networking.WebSockets;

public abstract class WebSocketSession : IHttpModule
{
    public string ID { get; } = RandomizeUtils.GenerateRandomString(12, CharacterType.AllOfIt);
    public long StartTime { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    protected HttpHeaderCollection Headers => Context.Request.Headers;
    protected IPEndPoint? EndPoint => Context.EndPoint;

    internal HttpServerContext Context { get; set; } = null!;

    private const string constant_key = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private ServerWebSocket socket = null!;
    private string? base64Key;

    public async Task Process(HttpServerContext ctx)
    {
        Context = ctx;

        var success = await acceptHandshake();

        if (!success)
        {
            Context.Close();
            return;
        }

        socket = new ServerWebSocket(ctx);
        socket.OnOpen += OnOpenInternal;
        socket.OnMessage += OnMessageInternal;
        socket.OnClose += OnCloseInternal;
        socket.StartListening();
    }

    #region Events

    internal virtual void OnOpenInternal() => OnOpen();
    internal virtual void OnMessageInternal(WebSocketMessage message) => OnMessage(message);
    internal virtual void OnCloseInternal() => OnClose();

    protected virtual void OnOpen()
    {
    }

    protected virtual void OnMessage(WebSocketMessage message)
    {
    }

    protected virtual void OnClose()
    {
    }

    protected virtual void OnError()
    {
    }

    protected virtual bool Authenticate(out string message)
    {
        message = "";
        return true;
    }

    #endregion

    #region Send

    public void Send(string text) => socket.SendText(text);
    public void Send(byte[] data) => socket.SendBinary(data);

    public async Task SendAsync(string text) => await socket.SendTextAsync(text);
    public async Task SendAsync(byte[] data) => await socket.SendBinaryAsync(data);

    #endregion

    #region Handshake

    private async Task<bool> acceptHandshake()
    {
        if (!validateRequest())
        {
            await replyError(HttpStatusCode.BadRequest, "Invalid request headers.");
            return false;
        }

        if (!Authenticate(out var msg))
        {
            await replyError(HttpStatusCode.Unauthorized, msg);
            return false;
        }

        base64Key = Context.Request.Headers["Sec-WebSocket-Key"];
        await replyHandshake();
        return true;
    }

    private async Task replyHandshake()
    {
        var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);
        res.Headers.Add("Upgrade", "websocket");
        res.Headers.Add("Connection", "Upgrade");
        res.Headers.Add("Sec-WebSocket-Accept", createResponseKey());
        await res.WriteToStream(Context.Stream);
    }

    private async Task replyError(HttpStatusCode code, string message)
    {
        var res = new HttpResponse(code);
        res.BodyStream.Write(Encoding.UTF8.GetBytes(message));
        await res.WriteToStream(Context.Stream);
    }

    private string createResponseKey()
    {
        var comb = base64Key + constant_key;
        var bytes = Encoding.UTF8.GetBytes(comb);
        var hash = SHA1.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool validateRequest()
    {
        return Context.Request.Headers.Contains("Connection", "Upgrade")
               && Context.Request.Headers.Contains("Upgrade", "websocket");
    }

    #endregion
}
