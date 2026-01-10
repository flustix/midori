using System.Text;
using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("g")]
public class DBusSignatureValue : IDBusValue<string>
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string Value { get; set; } = string.Empty;

    public void Read(Stream stream) => Value = stream.ReadStringNullByte(Encoding);
    public void Write(BinaryWriter writer) => writer.WriteStringNullByte(Value, Encoding);
}
