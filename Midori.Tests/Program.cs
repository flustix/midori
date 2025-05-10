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
        await client.StartWave();

        await Task.Delay(-1);
    }

    private class Client : IClient
    {
        public TypedWebSocketClient<IServer, IClient> Connection { get; }

        public Client()
        {
            Connection = new TypedWebSocketClient<IServer, IClient>(this) { PingInterval = 2000 };
            Connection.Connect("ws://127.0.0.1:9090/");
        }

        public async Task StartWave()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    await Connection.Server.Wave();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to wave.");
                }
            }
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

        public async Task<string> Wave()
        {
            var rng = Random.Shared.Next(0, 2);
            // Logger.Log($"{rng}");

            if (rng == 1)
            {
                Logger.Log("throwing");
                throw new TestException("yuh");
            }

            return await Task.FromResult("aa");
        }
    }

    private class TestException(string message) : TypedWebSocketException(message);
}

public interface IServer
{
    Task<string> Wave();
}

public interface IClient
{
    Task WaveBack();
}
