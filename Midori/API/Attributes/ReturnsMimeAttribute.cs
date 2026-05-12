namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ReturnsMimeAttribute : Attribute
{
    public string MimeType { get; }

    public ReturnsMimeAttribute(string mimeType)
    {
        MimeType = mimeType;
    }
}
