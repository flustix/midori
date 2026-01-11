namespace Midori.DBus.Tests;

public abstract class BaseConnectionTest
{
    protected DBusConnection Connection { get; private set; }

    [SetUp]
    public async Task Setup()
    {
        Connection = new DBusConnection(DBusAddress.Session);
        await Connection.Connect();
    }

    [TearDown]
    public async Task TearDown()
    {
        await Connection.Close();
    }
}
