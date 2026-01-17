namespace Midori.DBus.Tests;

public class TestSignals : BaseConnectionTest
{
    [Test]
    public async Task TestWatchNameChange()
    {
        var hasBeenCalled = false;

        var match = await Connection.AddMatch<(string, string, string)>(ev =>
        {
            var (name, o, n) = ev;
            Logger.Log($"{name} changed owner from {o} to {n}");
            hasBeenCalled = true;
        }, new DBusMatchRule(
            DBusMatchType.Signal,
            "org.freedesktop.DBus",
            "/org/freedesktop/DBus",
            "org.freedesktop.DBus",
            "NameOwnerChanged"
        ));

        await Task.Delay(2000);
        Assert.That(hasBeenCalled, Is.EqualTo(true));

        hasBeenCalled = false;
        match.Dispose();
        await Task.Delay(2000);
        Assert.That(hasBeenCalled, Is.EqualTo(false));
    }
}
