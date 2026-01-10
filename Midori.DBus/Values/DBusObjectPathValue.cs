using System.Text;
using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("o", 4, typeof(DBusObjectPath))]
public class DBusObjectPathValue : IDBusValue<DBusObjectPath>, IHasEncoding
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public DBusObjectPath Value { get; set; } = new(string.Empty);

    public void Read(Stream stream) => Value = stream.ReadStringNull(Encoding);
    public void Write(BinaryWriter writer) => writer.WriteStringNull(Value.ToString(), Encoding);
}
