using JetBrains.Annotations;

namespace Midori.DBus.Attributes;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Interface)]
public class DBusInterfaceAttribute : Attribute
{
    public string Interface { get; }

    public DBusInterfaceAttribute(string @interface)
    {
        Interface = @interface;
    }
}
