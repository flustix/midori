using System.Text;
using Midori.DBus.Values;
using Midori.Utils.Extensions;

namespace Midori.DBus.IO;

public class DBusWriter
{
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    internal MemoryStream Stream { get; }
    internal string Signature { get; private set; } = string.Empty;

    private BinaryWriter writer { get; }

    public DBusWriter()
    {
        Stream = new MemoryStream();
        writer = new BinaryWriter(Stream);
    }

    public void Pad(int p) => writer.PadTo(p);

    public void WriteByte(byte val) => writeValue(new DBusByteValue { Value = val });
    public void WriteBool(bool val) => writeValue(new DBusBoolValue { Value = val });

    public void WriteInt32(int val) => writeValue(new DBusInt32Value { Value = val });
    public void WriteUInt32(uint val) => writeValue(new DBusUInt32Value { Value = val });

    public void WriteString(string str, Encoding? enc = null) => writeValue(new DBusStringValue
    {
        Encoding = enc ?? DefaultEncoding,
        Value = str
    });

    public void WriteObjectPath(string str, Encoding? enc = null) => writeValue(new DBusObjectPathValue
    {
        Encoding = enc ?? DefaultEncoding,
        Value = str
    });

    public void WriteSignature(string str, Encoding? enc = null) => writeValue(new DBusSignatureValue
    {
        Encoding = enc ?? DefaultEncoding,
        Value = str
    });

    private void writeValue(IDBusValue val)
    {
        val.Write(writer);
        Signature += val.GetSignature();
    }
}
