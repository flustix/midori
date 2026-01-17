namespace Midori.DBus.Tests;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var test = new TestSignals();
        await test.Setup();
        await test.TestWatchNameChange();
        await test.TearDown();
    }
}
