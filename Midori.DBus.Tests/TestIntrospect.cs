namespace Midori.DBus.Tests;

public class TestIntrospect : BaseConnectionTest
{
    [Test]
    public async Task Test()
    {
        var intro = await Connection.Introspect("org.mpris.MediaPlayer2.playerctld", "/org/mpris/MediaPlayer2");
        Logger.Log(intro);
    }
}
