using Midori.DBus.Values;

namespace Midori.DBus.Tests;

public class TestMethodCalls : BaseConnectionTest
{
    [Test]
    public async Task TestRequestName()
    {
        await Connection.RequestName("moe.flux.Midori", 0);
    }

    [Test]
    public async Task TestListNames()
    {
        var names = await Connection.ListNames();
        names.ForEach(x => Logger.Log(x));
    }

    [Test]
    public async Task TestNameOwned()
    {
        var own = await Connection.NameHasOwner("moe.flux.Midori");
        Logger.Log($"Is owned: {own}");
    }

    [Test]
    public async Task TestObjectProperty()
    {
        var msg = await Connection.CallMethod("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus.Properties", "Get", w =>
        {
            w.WriteString("org.freedesktop.DBus");
            w.WriteString("Interfaces");
        });

        var read = msg.GetBodyReader();
        var sig = read.ReadSignature();

        switch (sig)
        {
            case "as":
                var interfaces = read.ReadArray<DBusStringValue, string>();
                interfaces.ForEach(x => Logger.Log(x));
                break;

            default:
                throw new InvalidOperationException();
        }
    }
}
