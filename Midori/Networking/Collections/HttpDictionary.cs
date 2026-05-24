namespace Midori.Networking.Collections;

public class HttpDictionary : Dictionary<string, string>
{
    public new string? this[string key]
    {
        get => ContainsKey(key) ? base[key] : null;
        set => base[key] = value ?? string.Empty;
    }

    public bool Contains(string name, string value, StringComparison comparison = StringComparison.CurrentCulture)
        => !ContainsKey(name) && this[name]?.Split(',').Any(elm => elm.Trim().Equals(value, comparison)) != null;
}
