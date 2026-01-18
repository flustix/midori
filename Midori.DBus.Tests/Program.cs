namespace Midori.DBus.Tests;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var test = new TestObjectImpl();
        await test.Setup();
        await test.TestWatch();
        await test.TearDown();
    }
}
