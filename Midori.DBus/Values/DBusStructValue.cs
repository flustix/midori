using Midori.DBus.Attributes;

namespace Midori.DBus.Values;

[DBusSignature("{", 8, typeof(ValueTuple<,>))]
public class DBusStructValue<T1, T2> : IDBusValue<(T1, T2)>, IDynamicSignature
{
    public (T1, T2) Value { get; set; } = default;

    public void Read(Stream stream)
    {
    }

    public void Write(BinaryWriter writer)
    {
        IDBusValue.GetForType(typeof(T1), Value.Item1!).Write(writer);
        IDBusValue.GetForType(typeof(T2), Value.Item2!).Write(writer);
    }

    public string GetSignature()
    {
        return "{"
               + IDBusValue.GetForType(typeof(T1)).GetDBusSignature()
               + IDBusValue.GetForType(typeof(T2)).GetDBusSignature()
               + "}";
    }
}
