using Midori.DBus.Attributes;

namespace Midori.DBus.Values;

[DBusSignature("y", 1, typeof(byte))]
public class DBusByteValue : IDBusValue<byte>
{
    public byte Value { get; set; }

    public void Read(Stream stream) => Value = (byte)stream.ReadByte();
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
