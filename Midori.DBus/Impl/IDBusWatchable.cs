namespace Midori.DBus.Impl;

public interface IDBusWatchable
{
    T GetPropertyValue<T>(string member);
    void StartWatching<T>(string member, Action<T> callback);
}
