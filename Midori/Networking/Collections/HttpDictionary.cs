namespace Midori.Networking.Collections;

public class HttpDictionary : Dictionary<string, string>
{
    public new string? this[string key]
    {
        get
        {
            var low = key.ToLowerInvariant();
            return ContainsKey(low) ? base[low] : null;
        }
        set => base[key.ToLowerInvariant()] = value ?? string.Empty;
    }

    public bool Contains(string name, string value, StringComparison comparison = StringComparison.CurrentCulture)
    {
        name = name.ToLowerInvariant();
        return !ContainsKey(name) && this[name]?.Split(',').Any(elm => elm.Trim().Equals(value, comparison)) != null;
    }
}
