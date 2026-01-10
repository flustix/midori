using Midori.Logging;

namespace Midori.DBus;

internal static class Program
{
    private static async Task Main()
    {
        var conn = new DBusConnection(DBusAddress.Session);
        await conn.Connect();

        await conn.RequestName("moe.flux.Midori", 0);

        var names = await conn.ListNames();
        names.ForEach(x => Logger.Log(x));

        var act = await conn.ListActivatableNames();
        act.ForEach(x => Logger.Log(x));

        var own = await conn.NameHasOwner("org.kde.StatusNotifierWatcher");
        Logger.Log($"Is owned: {own}");

        await Task.Delay(-1);
    }
}
