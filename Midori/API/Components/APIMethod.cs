namespace Midori.API.Components;

/// <summary>
/// An enum version of <see cref="System.Net.Http.HttpMethod"/>.
/// </summary>
public enum APIMethod
{
    Get,
    Put,
    Post,
    Delete,
    Head,
    Options,
    Trace,
    Patch,
    Connect
}

public static class APIMethodExtensions
{
    public static HttpMethod GetSystemNet(this APIMethod method) => method switch
    {
        APIMethod.Get => HttpMethod.Get,
        APIMethod.Put => HttpMethod.Put,
        APIMethod.Post => HttpMethod.Post,
        APIMethod.Delete => HttpMethod.Delete,
        APIMethod.Head => HttpMethod.Head,
        APIMethod.Options => HttpMethod.Options,
        APIMethod.Trace => HttpMethod.Trace,
        APIMethod.Patch => HttpMethod.Patch,
        APIMethod.Connect => HttpMethod.Connect,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };
}
