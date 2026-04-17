using System.Net;

namespace Midori.Networking;

public class HttpConfiguration
{
    public IPAddress Address { get; set; } = IPAddress.Loopback;
    public ushort Port { get; set; } = 8080;
}
