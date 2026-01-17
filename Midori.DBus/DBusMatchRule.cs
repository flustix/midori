namespace Midori.DBus;

public readonly struct DBusMatchRule : IEquatable<DBusMatchRule>
{
    public DBusMatchType Type { get; }
    public string Sender { get; }
    public DBusObjectPath Path { get; }
    public string Member { get; }
    public string Interface { get; }

    public DBusMatchRule(DBusMatchType type, string sender, DBusObjectPath path, string @interface, string member)
    {
        Type = type;
        Sender = sender;
        Path = path;
        Member = member;
        Interface = @interface;
    }

    public string Build()
    {
        var sw = new StringWriter();

        sw.Write($"type='{Type switch {
            DBusMatchType.Signal => "signal",
            DBusMatchType.MethodCall => "method_call",
            DBusMatchType.MethodReturn => "method_return",
            DBusMatchType.Error => "error",
            _ => throw new ArgumentOutOfRangeException()
        }}'");

        sw.Write($",sender='{Sender}'");
        sw.Write($",path='{Path}'");
        sw.Write($",interface='{Interface}'");
        sw.Write($",member='{Member}'");

        return sw.ToString();
    }

    #region IEquatable

    public bool Equals(DBusMatchRule other) => Type == other.Type && Sender == other.Sender && Path.Equals(other.Path) && Member == other.Member && Interface == other.Interface;
    public override bool Equals(object? obj) => obj is DBusMatchRule other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((int)Type, Sender, Path, Member, Interface);

    #endregion
}

public enum DBusMatchType
{
    Signal,
    MethodCall,
    MethodReturn,
    Error
}
