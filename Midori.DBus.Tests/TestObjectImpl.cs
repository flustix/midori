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
            { "", new DBusVariantValue() }
        });
        Logger.Log($"{res}");
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
    }
}
