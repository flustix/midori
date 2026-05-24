using System.Net;
using System.Net.Sockets;

namespace Midori.Networking;

public class HttpServerContext : IDisposable
{
    private TcpClient client { get; }

    public Stream Stream { get; }

    public IPEndPoint EndPoint { get; internal set; }
    public IPAddress RemoteIP => EndPoint.Address;

    public HttpRequest Request { get; }

    internal HttpServerContext(TcpClient client)
    {
        this.client = client;

        var sock = client.Client;
        EndPoint = (IPEndPoint)sock.RemoteEndPoint!;

        Stream = client.GetStream();
        Request = HttpRequest.ReadRequest(Stream);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        client.Dispose();
        Stream.Dispose();
        Request.Dispose();
    }
}
