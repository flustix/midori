namespace Midori.Networking.Collections;

public class HttpDictionary : Dictionary<string, string>
{
    public bool Contains(string name, string value, StringComparison comparison = StringComparison.CurrentCulture)
        => !ContainsKey(name) && this[name].Split(',').Any(elm => elm.Trim().Equals(value, comparison));

    public string? Get(string key) => ContainsKey(key) ? this[key] : null;
}
