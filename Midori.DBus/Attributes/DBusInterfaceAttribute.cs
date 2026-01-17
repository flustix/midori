using JetBrains.Annotations;

namespace Midori.DBus.Attributes;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class DBusInterfaceAttribute : Attribute
{
    public string Interface { get; }
    public bool AllAreMembers { get; } = false;

    public DBusInterfaceAttribute(string @interface)
    {
        Interface = @interface;
    }
}
