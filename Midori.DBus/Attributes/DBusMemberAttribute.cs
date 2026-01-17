namespace Midori.DBus.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class DBusMemberAttribute : Attribute
{
    public string? CustomName { get; } = null;
}
