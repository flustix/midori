using Midori.DBus.Attributes;
using Midori.DBus.Impl;

namespace Midori.DBus.Tests;

public class TestObjectImpl
{
    [Test]
    public async Task TestBuild()
    {
        var connection = new DBusConnection(DBusAddress.Session);
        await connection.Connect();

        var b = connection.CreateProxy<IBaseType>("org.freedesktop.DBus", "/org/freedesktop/DBus");
        var u = await b.RequestName("moe.flux.Midori", 0);
        Logger.Log($"{u}");

        await connection.Close();
    }

    [DBusInterface("org.freedesktop.DBus")]
    public interface IBaseType
    {
        Task<uint> RequestName(string name, uint flags);
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
