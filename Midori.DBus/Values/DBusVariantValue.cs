using Midori.DBus.Attributes;
using Midori.Utils.Extensions;

namespace Midori.DBus.Values;

[DBusSignature("v", 4, typeof(DBusVariantValue))]
public class DBusVariantValue : IDBusValue<IDBusValue>
{
    public DBusSignatureValue Signature { get; set; } = new();
    public IDBusValue Value { get; set; }

    public DBusVariantValue()
    {
        Value = null!;
    }

    public DBusVariantValue(DBusSignatureValue signature, IDBusValue value)
    {
        Signature = signature;
        Value = value;
    }

    public DBusVariantValue(object value)
    {
        Value = IDBusValue.GetForType(value.GetType());
        Signature = new DBusSignatureValue { Value = Value.GetDBusSignature() };
    }

    public void Read(Stream stream)
    {
    }

    public void Write(BinaryWriter writer)
    {
        Signature.Write(writer);
        writer.PadTo(Value.GetDBusAlignment());
        Value.Write(writer);
    }
}
