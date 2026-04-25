namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class SourceAttribute : Attribute
{
    public ParameterSource Source { get; }

    public SourceAttribute(ParameterSource source)
    {
        Source = source;
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
