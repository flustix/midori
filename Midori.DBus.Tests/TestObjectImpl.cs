using Midori.DBus.Attributes;
using Midori.DBus.Impl;
using Midori.DBus.Values;

namespace Midori.DBus.Tests;

public class TestObjectImpl : BaseConnectionTest
{
    [Test]
    public async Task TestRequestName()
    {
        var b = Connection.CreateProxy<IBaseType>("org.freedesktop.DBus", "/org/freedesktop/DBus");
        var u = await b.RequestName("moe.flux.Midori", 0);
        Logger.Log($"{u}");
    }

    [Test]
    public async Task TestPickFile()
    {
        var file = Connection.CreateProxy<IFileChooser>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
        var res = await file.OpenFile("/Midori", "title", new Dictionary<string, DBusVariantValue>
        {
            { "multiple", new DBusVariantValue(true) }
        });
        Logger.Log($"{res}");
    }

    [Test]
    public async Task TestWatch()
    {
        var player = Connection.CreateProxy<IMediaPlayer>(":1.97053", "/org/mpris/MediaPlayer2");

        player.StartWatching<string>("PlaybackStatus", t => Logger.Log($"Playback status changed to {t}."));
        player.StartWatching<Dictionary<string, DBusVariantValue>>("Metadata", t =>
        {
            Logger.Log($"metadata changed {t.Count}");

            foreach (var (key, value) in t)
            {
                Logger.Log($"  {key} -> {value.Value.Value}");
            }
        });

        var position = player.GetPropertyValue<long>("Position");
        Logger.Log($"pos is {TimeSpan.FromMilliseconds(position / 1000f)}");

        await player.PlayPause();
        await Task.Delay(500);
        await player.PlayPause();
        await Task.Delay(-1);
    }

    [DBusInterface("org.freedesktop.DBus")]
    public interface IBaseType
    {
        Task<uint> RequestName(string name, uint flags);
    }

    [DBusInterface("org.freedesktop.portal.FileChooser")]
    public interface IFileChooser
    {
        Task<DBusObjectPath> OpenFile(string parent, string title, Dictionary<string, DBusVariantValue> options);
    }

    [DBusInterface("org.mpris.MediaPlayer2.Player")]
    public interface IMediaPlayer : IDBusWatchable
    {
        // string PlaybackStatus { get; }

        Task Next();
        Task OpenUri(string uri);
        Task Pause();
        Task Play();
        Task PlayPause();
        Task Previous();

        // Task Seek(long position);
        Task Stop();
    }

    /// <summary>
    /// testing type for IL implementation
    /// </summary>
    internal class ImplementedType : DBusObject, IBaseType
    {
        private readonly DBusConnection connection;

        public ImplementedType(DBusConnection connection)
        {
            this.connection = connection;
        }

        public Task<uint> RequestName(string name, uint flags)
        {
            var list = new List<object>();
            list.Add(name);
            list.Add(flags);

            var result = connection.CallFromProxy(this, "", list).Result;
            return Task.FromResult(connection.GetReturnForProxy<uint>(this, "", result));
        }

        public Task NoReturn(string name, uint flags)
        {
            var list = new List<object>();
            list.Add(name);
            list.Add(flags);

            var result = connection.CallFromProxy(this, "", list).Result;
            return Task.CompletedTask;
        }
    }
}
