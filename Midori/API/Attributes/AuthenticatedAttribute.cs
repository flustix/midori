namespace Midori.API.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class AuthenticatedAttribute : Attribute
{
    public bool Required { get; set; } = true;
    public Type? Authenticator { get; set; } = null;
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public AuthenticatedAttribute()
    {
    }

    public AuthenticatedAttribute(params string[] scopes)
    {
        Scopes = scopes;
    }
}
