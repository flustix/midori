using System.Net;
using System.Net.Sockets;

namespace Midori.Networking;

public class HttpServerContext : IDisposable
{
    private TcpClient client { get; }

    internal Stream Stream { get; }

    public IPEndPoint? EndPoint { get; }
    public HttpRequest Request { get; }

    public HttpServerContext(TcpClient client)
    {
        this.client = client;

        var sock = client.Client;
        EndPoint = (IPEndPoint)sock.RemoteEndPoint!;

        Stream = client.GetStream();
        Request = HttpRequest.ReadRequest(Stream);
    }

    public void Close()
    {
        client.Close();
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        client.Dispose();
        Stream.Dispose();
    }
}
