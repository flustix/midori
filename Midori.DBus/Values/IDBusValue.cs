using System.Reflection;
using Midori.DBus.Attributes;

namespace Midori.DBus.Values;

public interface IDBusValue
{
    void Read(Stream stream);
    void Write(BinaryWriter writer);

    private static readonly Dictionary<string, Type> signature_mapping = new();

    static IDBusValue()
    {
        var types = typeof(IDBusValue).Assembly.GetTypes()
                                      .Where(x => x.GetInterfaces().Contains(typeof(IDBusValue)))
                                      .Where(x => x.IsClass);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<DBusSignatureAttribute>() ?? throw new InvalidOperationException("Value does not have signature attribute.");
            signature_mapping[attr.Signature] = type;
        }
    }

    public static IDBusValue GetForSignature(string sig) => (Activator.CreateInstance(signature_mapping[sig]) as IDBusValue)!;
}

public interface IDBusValue<T> : IDBusValue
{
    T Value { get; set; }
}

public static class DBusValueExtensions
{
    public static byte? AsByte(this IDBusValue? val) => val.getAsS<DBusByteValue, byte>();
    public static bool? AsBool(this IDBusValue? val) => val.getAsS<DBusBoolValue, bool>();
    public static int? AsInt32(this IDBusValue? val) => val.getAsS<DBusInt32Value, int>();
    public static uint? AsUInt32(this IDBusValue? val) => val.getAsS<DBusUInt32Value, uint>();
    public static string? AsString(this IDBusValue? val) => val.getAsC<DBusStringValue, string>();
    public static string? AsObjectPath(this IDBusValue? val) => val.getAsC<DBusObjectPathValue, string>();
    public static string? AsSignature(this IDBusValue? val) => val.getAsC<DBusSignatureValue, string>();

    private static T? getAsS<V, T>(this IDBusValue? value)
        where V : class, IDBusValue<T>
        where T : struct
    {
        var cast = value as V;
        return cast?.Value;
    }

    private static T? getAsC<V, T>(this IDBusValue? value)
        where V : class, IDBusValue<T>
        where T : class
    {
        var cast = value as V;
        return cast?.Value;
    }

    public static string GetSignature(this IDBusValue val)
    {
        var type = val.GetType();
        var attr = type.GetCustomAttribute<DBusSignatureAttribute>() ?? throw new InvalidOperationException("Value does not have signature attribute.");
        return attr.Signature;
    }
}
