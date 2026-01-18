using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("d", 8, typeof(double))]
public class DBusDouble : IDBusValue<double>
{
    public double Value { get; set; }

    public void Read(Stream stream) => Value = stream.ReadDouble();
    public void Write(BinaryWriter writer) => writer.Write(Value);
}
