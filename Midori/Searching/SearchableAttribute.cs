namespace Midori.Searching;

[AttributeUsage(AttributeTargets.Property)]
public class SearchableAttribute : Attribute
{
    public string Key { get; }

    public SearchableAttribute(string key)
    {
        Key = key;
    }
}
