namespace Midori.DBus.Impl;

public interface IDBusWatchable : IDisposable
{
    string Path { get; }

    T GetPropertyValue<T>(string member);
    void StartWatching<T>(string member, Action<T> callback);
    IDisposable ListenToSignal<T>(string member, Action<T> callback);
}
