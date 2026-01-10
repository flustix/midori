using System.Text;
using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("g", 1)]
public class DBusSignatureValue : IDBusValue<string>, IHasEncoding
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string Value { get; set; } = string.Empty;

    public void Read(Stream stream) => Value = stream.ReadStringNullByte(Encoding);
    public void Write(BinaryWriter writer) => writer.WriteStringNullByte(Value, Encoding);
}
