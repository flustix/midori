using System.Text;
using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("o", 4)]
public class DBusObjectPathValue : IDBusValue<string>, IHasEncoding
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string Value { get; set; } = string.Empty;

    public void Read(Stream stream) => Value = stream.ReadStringNull(Encoding);
    public void Write(BinaryWriter writer) => writer.WriteStringNull(Value, Encoding);
}
