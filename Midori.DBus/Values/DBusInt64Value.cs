using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("x", 8, typeof(long))]
public class DBusInt64Value : IDBusValue<long>
{
    public long Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadInt64();
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
