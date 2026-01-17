namespace Midori.DBus;

public class DBusObjectPath : IEquatable<DBusObjectPath>
{
    private readonly string value;

    public DBusObjectPath(string value)
    {
        this.value = value;
    }

    public bool StartsWith(DBusObjectPath path) => value.StartsWith(path.value);

    public override string ToString() => value;

    public static implicit operator DBusObjectPath(string s) => new(s);
    public static implicit operator string?(DBusObjectPath? p) => p?.value;

    #region IEquatable

    public bool Equals(DBusObjectPath? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return value == other.value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((DBusObjectPath)obj);
    }

    public override int GetHashCode() => value.GetHashCode();

    #endregion
}
