using Midori.Networking;
using NUnit.Framework;

namespace Midori.Tests.Preset;

public abstract class BaseAPITest
{
    protected HttpServer Server { get; private set; }

    [SetUp]
    public void Setup()
    {
        // Server = new HttpServer();
    }

    protected void Start()
    {
        // Server.Start(IPAddress.Any, 9090);
    }

    [TearDown]
    public void TearDown()
    {
        // Server.Close();
    }
}
