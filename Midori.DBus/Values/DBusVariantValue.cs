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
        Value = IDBusValue.GetForType(value.GetType(), value);
        Signature = new DBusSignatureValue { Value = Value.GetDBusSignature() };
    }

    public void Read(Stream stream)
    {
        Signature = new DBusSignatureValue();
        Signature.Read(stream);

        Value = IDBusValue.GetForSignature(Signature.Value);
        stream.AlignRead((uint)stream.Position, IDBusValue.IsStruct(Value) ? 4 : Value.GetDBusAlignment());
        Value.Read(stream);
    }

    public void Write(BinaryWriter writer)
    {
        Signature.Write(writer);
        writer.PadTo(Value.GetDBusAlignment());
        Value.Write(writer);
    }
}
