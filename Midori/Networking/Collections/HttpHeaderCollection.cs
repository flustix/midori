namespace Midori.Networking.Collections;

public class HttpHeaderCollection : HttpDictionary
{
    internal void AddLine(string header)
    {
        var idx = header.IndexOf(':');

        if (idx == -1)
            throw new ArgumentException("Header does not contain a colon character.");

        var name = header[..idx].Trim().ToLowerInvariant();
        var value = idx < header.Length - 1 ? header[(idx + 1)..] : string.Empty;

        this[name] = value.Trim();
    }
}
