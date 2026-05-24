using System.Web;

namespace Midori.Networking.Collections;

public class HttpCookieCollection : HttpDictionary
{
    internal void AddContent(string content)
    {
        foreach (var entry in content.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            AddPart(entry);
        }
    }

    internal void AddPart(string entry)
    {
        var idx = entry.IndexOf('=');

        if (idx == -1)
            throw new ArgumentException("Cookie entry does not contain an equals character.");

        var name = entry[..idx].Trim().ToLowerInvariant();
        var value = idx < entry.Length - 1 ? entry[(idx + 1)..] : string.Empty;

        this[name] = HttpUtility.UrlDecode(value.Trim());
    }
}
