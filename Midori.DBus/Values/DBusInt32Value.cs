using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("i")]
public class DBusInt32Value : IDBusValue<int>
{
    public int Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadInt32();
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
