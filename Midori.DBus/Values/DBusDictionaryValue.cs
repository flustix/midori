using Midori.DBus.Attributes;

namespace Midori.DBus.Values;

[DBusSignature("a{}", 8, typeof(Dictionary<,>))]
public class DBusDictionaryValue<T1, T2> : IDBusValue<Dictionary<T1, T2>>, IDynamicSignature
    where T1 : notnull
{
    public Dictionary<T1, T2> Value { get; set; } = new();

    public void Read(Stream stream)
    {
        // TODO: missing read
    }

    public void Write(BinaryWriter writer)
    {
        // looks stupid but I don't need to copy-paste stuff
        var arr = new DBusArray<(T1, T2)> { Value = Value.Select(x => (x.Key, x.Value)).ToList() };
        arr.Write(writer);
    }

    public string GetSignature()
    {
        return "a{"
               + IDBusValue.GetForType(typeof(T1)).GetDBusSignature()
               + IDBusValue.GetForType(typeof(T2)).GetDBusSignature()
               + "}";
    }
}
