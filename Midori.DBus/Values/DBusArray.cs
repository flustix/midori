using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("a", 4, typeof(List<>))]
public class DBusArray<T> : IDBusValue<List<T>>, IDynamicSignature
{
    public List<T> Value { get; set; } = [];

    public void Read(Stream stream)
    {
        Value = [];
        var current = stream.Position;
        var len = stream.ReadUInt32();

        while (stream.Position < current + len)
        {
            var t = IDBusValue.GetForType(typeof(T));
            stream.AlignRead((uint)stream.Position, t.GetDBusAlignment());
            t.Read(stream);
            Value.Add((T)t.Value);
        }
    }

    public void Write(BinaryWriter writer)
    {
        var start = writer.BaseStream.Position;

        writer.Write((uint)0);
        writer.PadTo(IDBusValue.GetForType(typeof(T)).GetDBusAlignment());

        var dataStart = writer.BaseStream.Position;

        foreach (var val in Value)
        {
            var dval = IDBusValue.GetForType(typeof(T), val!);
            writer.PadTo(dval.GetDBusAlignment());
            dval.Write(writer);
        }

        var current = writer.BaseStream.Position;
        var diff = current - dataStart;

        writer.BaseStream.Seek(start, SeekOrigin.Begin);
        writer.Write((uint)diff);
        writer.BaseStream.Seek(current, SeekOrigin.Begin);
    }

    public string GetSignature() => "a" + IDBusValue.GetForType(typeof(T)).GetDBusSignature();
}
