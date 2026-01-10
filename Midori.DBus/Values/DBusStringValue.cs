using System.Text;
using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("s", 4, typeof(string))]
public class DBusStringValue : IDBusValue<string>, IHasEncoding
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string Value { get; set; } = string.Empty;

    public void Read(Stream stream) => Value = stream.ReadStringNull(Encoding);
    public void Write(BinaryWriter writer) => writer.WriteStringNull(Value, Encoding);
}
