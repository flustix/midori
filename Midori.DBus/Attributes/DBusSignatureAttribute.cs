using JetBrains.Annotations;

namespace Midori.DBus.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class DBusSignatureAttribute : Attribute
{
    public string Signature { get; }
    public int Alignment { get; }

    public DBusSignatureAttribute(string signature, int align)
    {
        Signature = signature;
        Alignment = align;
    }
}
