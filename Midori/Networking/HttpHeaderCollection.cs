using System.Collections.Specialized;

namespace Midori.Networking;

public class HttpHeaderCollection : NameValueCollection
{
    internal void AddLine(string header)
    {
        var idx = header.IndexOf(':');

        if (idx == -1)
            throw new ArgumentException("Header does not contain a color character.");

        var name = header[..idx];
        var value = idx < header.Length - 1 ? header[(idx + 1)..] : string.Empty;

        base.Set(name.Trim(), value.Trim());
    }

    public bool Contains(string name, string value, StringComparison comparison = StringComparison.CurrentCulture)
    {
        var val = this[name];
        return val != null && val.Split(',').Any(elm => elm.Trim().Equals(value, comparison));
    }
}
