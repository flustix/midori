namespace Midori.DBus;

public class DBusObjectPath
{
    private readonly string value;

    public DBusObjectPath(string value)
    {
        this.value = value;
    }

    public override string ToString() => value;

    public static implicit operator DBusObjectPath(string s) => new(s);
    public static implicit operator string?(DBusObjectPath? p) => p?.value;
}
