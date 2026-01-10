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

    public string ReadString(Encoding? enc = null)
    {
        enc ??= DefaultEncoding;
        return readValue<DBusStringValue>(x => x.Encoding = enc).Value;
    }

    public string ReadSignature(Encoding? enc = null)
    {
        enc ??= DefaultEncoding;
        return readValue<DBusSignatureValue>(x => x.Encoding = enc).Value;
    }

    public byte ReadByte() => readValue<DBusByteValue>().Value;

    public uint ReadUInt32() => readValue<DBusUInt32Value>().Value;

    private T readValue<T>(Action<T>? before = null)
        where T : IDBusValue, new()
    {
        var t = new T();
        t.Read(Stream);
        return t;
    }
}
