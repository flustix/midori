namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ExpectsMimeAttribute : Attribute
{
    public string MimeType { get; }

    public ExpectsMimeAttribute(string mimeType)
    {
        MimeType = mimeType;
    }
}
