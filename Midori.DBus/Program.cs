using Midori.DBus.Exceptions;
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

        var own = await conn.NameHasOwner("moe.flux.Midori");
        Logger.Log($"Is owned: {own}");

        try
        {
            var msg = await conn.CallMethod("org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher", "org.freedesktop.DBus.Properties", "Get", w =>
            {
                w.WriteString("org.kde.StatusNotifierWatcher");
                w.WriteString("ProtocolVersion");
            });

            var read = msg.GetBodyReader();
            Logger.Log($"SNW ProtoVersion: {read.ReadInt32()}");
        }
        catch (DBusException ex)
        {
            Logger.Error(ex, "Failed to get ProtoVersion from SNW.");
        }

        await Task.Delay(-1);
    }
}
