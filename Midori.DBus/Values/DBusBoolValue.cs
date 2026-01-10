using Midori.DBus.Attributes;

namespace Midori.DBus.Values;

[DBusSignature("b")]
public class DBusBoolValue : IDBusValue<bool>
{
    public bool Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadByte() == 1;
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
