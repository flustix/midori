namespace Midori.Networking.WebSockets;

public class ServerWebSocket : WebSocket
{
    protected override bool MaskData => false;

    private HttpServerContext context { get; }

    public ServerWebSocket(HttpServerContext context)
        : base(context.Stream)
    {
        this.context = context;
    }

    public void StartListening() => Open();

    public override void Dispose()
    {
        base.Dispose();
        context.Close();
    }

    public override string ToString() => $"{context.EndPoint} {base.ToString()}";
}
