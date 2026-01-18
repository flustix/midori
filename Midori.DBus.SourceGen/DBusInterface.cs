namespace Midori.DBus.SourceGen;

public class DBusInterface
{
    public string Name { get; }
    public string Content { get; }

    public DBusInterface(string name, string content)
    {
        Name = name;
        Content = content;
    }
}
