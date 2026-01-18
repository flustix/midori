using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("(", 8, typeof(ValueTuple<,>))]
public class DBusStructValue<T1, T2> : IDBusValue<(T1, T2)>, IDynamicSignature
{
    public (T1, T2) Value { get; set; } = default;

    public void Read(Stream stream)
    {
        Value = (
            IDBusValue.ReadStructPart<T1>(stream),
            IDBusValue.ReadStructPart<T2>(stream, typeof(T2) != typeof(DBusVariantValue))
        );
    }

    public void Write(BinaryWriter writer)
    {
        var v1 = IDBusValue.GetForType(typeof(T1), Value.Item1!);
        writer.PadTo(v1.GetDBusAlignment());
        v1.Write(writer);

        var v2 = IDBusValue.GetForType(typeof(T2), Value.Item2!);

        // I spent a whole day figuring out why dictionaries don't work properly
        // and this is the reason...
        // why even put alignments if we don't use them everywhere
        if (v2 is not DBusVariantValue)
            writer.PadTo(v2.GetDBusAlignment());

        v2.Write(writer);
    }

    public string GetSignature()
    {
        return "("
               + IDBusValue.GetForType(typeof(T1)).GetDBusSignature()
               + IDBusValue.GetForType(typeof(T2)).GetDBusSignature()
               + ")";
    }
}

[DBusSignature("(", 8, typeof(ValueTuple<,,>))]
public class DBusStructValue<T1, T2, T3> : IDBusValue<(T1, T2, T3)>, IDynamicSignature
{
    public (T1, T2, T3) Value { get; set; } = default;

    public void Read(Stream stream)
    {
        Value = (
            IDBusValue.ReadStructPart<T1>(stream),
            IDBusValue.ReadStructPart<T2>(stream),
            IDBusValue.ReadStructPart<T3>(stream)
        );
    }

    public void Write(BinaryWriter writer)
    {
        var v1 = IDBusValue.GetForType(typeof(T1), Value.Item1!);
        writer.PadTo(v1.GetDBusAlignment());
        v1.Write(writer);

        var v2 = IDBusValue.GetForType(typeof(T2), Value.Item2!);
        writer.PadTo(v2.GetDBusAlignment());
        v2.Write(writer);

        var v3 = IDBusValue.GetForType(typeof(T3), Value.Item3!);
        writer.PadTo(v3.GetDBusAlignment());
        v3.Write(writer);
    }

    public string GetSignature()
    {
        return "("
               + IDBusValue.GetForType(typeof(T1)).GetDBusSignature()
               + IDBusValue.GetForType(typeof(T2)).GetDBusSignature()
               + IDBusValue.GetForType(typeof(T3)).GetDBusSignature()
               + ")";
    }
}
