using JetBrains.Annotations;

namespace Midori.API.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class ControllerAttribute : Attribute
{
    public string Prefix { get; }

    public ControllerAttribute(string prefix = "/")
    {
        Prefix = prefix;
    }
}
