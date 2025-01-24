using System.Net;
using Midori.Logging;
using Midori.Networking;
using Midori.Networking.WebSockets.Typed;

namespace Midori.Tests;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var server = new HttpServer();
        server.MapModule<Socket>("/");
        server.Start(IPAddress.Loopback, 9090);

        var client = new Client();

        await Task.Delay(-1);
    }

    private class Client : IClient
    {
        private TypedWebSocketClient<IServer, IClient> client { get; }

        public Client()
        {
            client = new TypedWebSocketClient<IServer, IClient>(this);
            client.Connect("ws://localhost:9090/");
        }

        public async Task Hi()
        {
            Logger.Log("server said hi");
            var type = await client.Server.Hello();
            Logger.Log($"server sent us: {type.Text}");
        }
    }

    private class Socket : TypedWebSocketSession<IServer, IClient>, IServer
    {
        protected override void OnOpen()
        {
            base.OnOpen();
            Client.Hi();
        }

        public Task<CustomType> Hello()
        {
            return Task.FromResult(new CustomType { Text = "ww" });
        }
    }
}

public class CustomType
{
    public string Text { get; set; } = string.Empty;
}

public interface IServer
{
    Task<CustomType> Hello();
}

public interface IClient
{
    Task Hi();
}
