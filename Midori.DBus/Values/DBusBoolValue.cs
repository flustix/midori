using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("b", 4)]
public class DBusBoolValue : IDBusValue<bool>
{
    public bool Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadUInt32() == 1;
    public void Write(BinaryWriter writer) => writer.Write((uint)(Value ? 1 : 0));
}
