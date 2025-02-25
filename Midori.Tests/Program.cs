using System.Net;
using Midori.API;
using Midori.API.Components;
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
        server.MapModule<APIServer<APIInteraction>>("/a");
        server.Start(IPAddress.Any, 9090);

        var clients = new List<Client>();

        for (int i = 0; i < 24; i++)
            clients.Add(new Client());

        await Task.Delay(-1);
    }

    private class Client : IClient
    {
        private TypedWebSocketClient<IServer, IClient> client { get; }

        public Client()
        {
            client = new TypedWebSocketClient<IServer, IClient>(this);
            client.Connect("ws://127.0.0.1:9090/");
        }

        public void Close()
        {
            client.Dispose();
        }

        public async Task Hi()
        {
            Logger.Log("server said hi");
            var type = await client.Server.Hello();
            Logger.Log($"server sent us: {type.Text}");
            Close();
        }
    }

    private class Socket : TypedWebSocketSession<IServer, IClient>, IServer
    {
        protected override bool Authenticate(out string message)
        {
            message = "test error";
            return false;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Client.Hi();
        }

        protected override void OnClose()
        {
            base.OnClose();
            Logger.Log("closing connection");
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
