namespace Midori.DBus.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DBusSignatureAttribute : Attribute
{
    public string Signature { get; }

    public DBusSignatureAttribute(string signature)
    {
        Signature = signature;
    }
}
