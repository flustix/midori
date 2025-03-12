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

        var client = new Client();
        client.Close();
    }

    private class Client : IClient
    {
        public TypedWebSocketClient<IServer, IClient> Connection { get; }

        public Client()
        {
            Connection = new TypedWebSocketClient<IServer, IClient>(this) { PingInterval = 2000 };
            Connection.Connect("ws://127.0.0.1:9090/");
        }

        public void Close()
        {
            Connection.Dispose();
        }

        public Task WaveBack()
        {
            Logger.Log($"we got a wave back!!");
            return Task.CompletedTask;
        }
    }

    private class Socket : TypedWebSocketSession<IServer, IClient>, IServer
    {
        protected override bool Authenticate(out string message)
        {
            message = "test error";
            return true;
        }

        protected override void OnClose()
        {
            base.OnClose();
            Logger.Log("closing connection");
        }

        public async Task Wave()
        {
            await Client.WaveBack();
        }
    }
}

public interface IServer
{
    Task Wave();
}

public interface IClient
{
    Task WaveBack();
}
