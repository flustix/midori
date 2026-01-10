using System.Text;
using Midori.DBus.Values;
using Midori.Utils.Extensions;

namespace Midori.DBus.IO;

public class DBusReader
{
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    internal MemoryStream Stream { get; }
    public long StreamPosition => Stream.Position;

    internal DBusReader(byte[] bytes)
    {
        Stream = new MemoryStream(bytes);
        Stream.Seek(0, SeekOrigin.Begin);
    }

    public void Align(int p) => Stream.AlignRead((uint)Stream.Position, p);

    public byte ReadByte() => readValue<DBusByteValue>().Value;
    public bool ReadBool() => readValue<DBusBoolValue>().Value;

    public int ReadInt32() => readValue<DBusInt32Value>().Value;
    public uint ReadUInt32() => readValue<DBusUInt32Value>().Value;

    public string ReadString(Encoding? enc = null)
    {
        enc ??= DefaultEncoding;
        return readValue<DBusStringValue>(x => x.Encoding = enc).Value;
    }

    public string ReadObjectPath(Encoding? enc = null)
    {
        enc ??= DefaultEncoding;
        return readValue<DBusObjectPathValue>(x => x.Encoding = enc).Value;
    }

    public string ReadSignature(Encoding? enc = null)
    {
        enc ??= DefaultEncoding;
        return readValue<DBusSignatureValue>(x => x.Encoding = enc).Value;
    }

    public List<V> ReadArray<T, V>()
        where T : IDBusValue<V>, new()
    {
        var len = ReadUInt32();
        var list = new List<V>();

        while (StreamPosition < len)
        {
            list.Add(readValue<T>(x =>
            {
                if (x is IHasEncoding e)
                    e.Encoding = DefaultEncoding;
            }).Value);
        }

        return list;
    }

    private T readValue<T>(Action<T>? before = null)
        where T : IDBusValue, new()
    {
        var t = new T();
        before?.Invoke(t);
        Align(t.GetAlignment());
        t.Read(Stream);
        return t;
    }

    internal object Read(IDBusValue val)
    {
        if (val is IHasEncoding enc)
            enc.Encoding = DefaultEncoding;

        Align(val.GetAlignment());
        val.Read(Stream);
        return val.Value;
    }
}
