using Midori.DBus.Attributes;

namespace Midori.DBus.Tests;

public class TestCallables : BaseConnectionTest
{
    [Test]
    public async Task TestRegister()
    {
        await Connection.RequestName("moe.flux.Midori", 0);

        // busctl --user call moe.flux.Midori /TestInterface moe.flux.Midori.TestInterface CallWithReturnDict as 2 "one" "two"

        var intr = new TestInterface();
        Connection.RegisterInterface(intr, "/TestInterface");
        await intr.CompletedTask.Task;
        await Task.Delay(2000); // wait for reply to be written
    }

    [DBusInterface("moe.flux.Midori.TestInterface")]
    public class TestInterface
    {
        public readonly TaskCompletionSource CompletedTask = new();

        [DBusMember]
        public int ReadOnly => 69;

        [DBusMember]
        public string ReadWrite { get; set; } = "very sick";

        [DBusMember]
        public void CallMethod(string input, DBusMessage message)
        {
            Logger.Log($"{nameof(CallMethod)} called with {input}");
            finish();
        }

        [DBusMember]
        public Task<(string, int)> CallWithReturn(List<string> strings, DBusMessage message)
        {
            Logger.Log($"{nameof(CallWithReturn)} called with {strings.Count} strings");
            finish();
            return Task.FromResult((string.Join(" ", strings), strings.Count));
        }

        [DBusMember]
        public Task<Dictionary<string, int>> CallWithReturnDict(List<string> strings, DBusMessage message)
        {
            Logger.Log($"{nameof(CallWithReturnDict)} called with {strings.Count} strings");
            finish();
            return Task.FromResult(strings.ToDictionary(x => x, x => x.Length));
        }

        private void finish() => CompletedTask.SetResult();
    }
}
