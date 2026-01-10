using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("u", 4)]
public class DBusUInt32Value : IDBusValue<uint>
{
    public uint Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadUInt32();
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
