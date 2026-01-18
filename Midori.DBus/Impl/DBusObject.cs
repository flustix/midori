using System.Runtime.CompilerServices;
using Midori.DBus.Values;

[assembly: InternalsVisibleTo("Midori.DBus.TypeImpls")]
[assembly: InternalsVisibleTo("Midori.DBus.Tests")]

namespace Midori.DBus.Impl;

internal class DBusObject : IDBusWatchable, IDisposable
{
    internal DBusConnection Connection { get; set; } = null!;
    internal string Destination { get; set; } = null!;
    internal string Path { get; set; } = null!;
    internal string Interface { get; set; } = null!;

    private IDisposable? watchPropertiesChange;
    private readonly List<(string prop, Action<DBusVariantValue>)> callbacks = new();

    internal void RegisterListeners()
    {
        watchPropertiesChange = Connection.AddMatch<(string, Dictionary<string, DBusVariantValue>, List<string>)>(propertiesChanged, new DBusMatchRule(
            DBusMatchType.Signal,
            Destination,
            Path,
            "org.freedesktop.DBus.Properties",
            "PropertiesChanged"
        ));
    }

    private void propertiesChanged((string, Dictionary<string, DBusVariantValue>, List<string>) ev)
    {
        var (intf, props, ls) = ev;

        if (intf != Interface)
            return;

        foreach (var (key, variant) in props)
        {
            foreach (var (_, act) in callbacks.Where(x => x.prop == key))
            {
                act.Invoke(variant);
            }
        }
    }

    public T GetPropertyValue<T>(string member) => Connection.GetProperty<T>(Destination, Path, Interface, member).Result;
    public void StartWatching<T>(string member, Action<T> callback) => callbacks.Add((member, v => callback.Invoke((T)v.Value.Value)));

    public void Dispose()
    {
        watchPropertiesChange?.Dispose();
    }
}
