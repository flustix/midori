namespace Midori.DBus;

public class DBusAddress
{
    private static DBusAddress? session;

    public static DBusAddress Session
    {
        get
        {
            if (session != null)
                return session;

            var env = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS") ?? throw new InvalidOperationException("DBus address not found in environment variables.");
            session = parseString(env);
            return session;
        }
    }

    public DBusAddressProto Protocol { get; }
    public string Path { get; }

    public DBusAddress(DBusAddressProto protocol, string path)
    {
        Protocol = protocol;
        Path = path;
    }

    private static DBusAddress parseString(string input)
    {
        // maybe properly implement multiple addresses?
        input = input.Split(";").First();

        var colSplit = input.Split(":");
        var proto = colSplit[0];
        var values = colSplit[1].Split(",").ToDictionary(x => x.Split("=").First(), x => x.Split("=").Last());

        return new DBusAddress(proto switch
        {
            "unix" => DBusAddressProto.Unix,
            _ => throw new InvalidOperationException($"unknown proto {proto}")
        }, values["path"]);
    }
}

public enum DBusAddressProto
{
    Unix
}
