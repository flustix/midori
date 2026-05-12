namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class SourceAttribute : Attribute
{
    public string? Name { get; set; }
    public ParameterSource Source { get; }

    public SourceAttribute(ParameterSource source)
    {
        Source = source;
    }

    public SourceAttribute(ParameterSource source, string name)
        : this(source)
    {
        Name = name;
    }
}

public enum ParameterSource
{
    Path,
    Query,
    Headers,
    Body,
    Form
}
