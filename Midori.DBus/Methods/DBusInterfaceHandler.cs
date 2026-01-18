using System.Reflection;
using Midori.DBus.Attributes;
using Midori.DBus.Exceptions;
using Midori.DBus.Values;
using Midori.Logging;

namespace Midori.DBus.Methods;

public interface IDBusInterfaceHandler
{
    void Handle(DBusMessage message);
    void WriteIntrospect(StringWriter sw);

    IDBusValue GetProperty(string member);
    Dictionary<string, DBusVariantValue> GetAllProperties();
}

internal class DBusInterfaceHandler : IDBusInterfaceHandler
{
    private readonly DBusConnection connection;
    private readonly string name;
    private readonly object target;

    private Dictionary<string, MethodInfo> methods { get; }
    private Dictionary<string, PropertyInfo> properties { get; }

    public DBusInterfaceHandler(DBusConnection connection, string name, bool allAreMember, object target)
    {
        this.connection = connection;
        this.name = name;
        this.target = target;

        (methods, properties) = getMembers(target, allAreMember);
    }

    public void Handle(DBusMessage message)
    {
        var member = message.Member;

        if (!methods.TryGetValue(member, out var method))
        {
            connection.QueueMessage(message.CreateError(new DBusException("Member does not exist.")));
            return;
        }

        try
        {
            var body = message.GetBodyReader();
            var args = new List<object>();

            foreach (var parameter in method.GetParameters())
            {
                switch (parameter.ParameterType.FullName ?? "")
                {
                    case "Midori.DBus.DBusMessage":
                        args.Add(message);
                        break;

                    case "Midori.DBus.DBusConnection":
                        args.Add(connection);
                        break;

                    default:
                        var dval = IDBusValue.GetForType(parameter.ParameterType);
                        args.Add(body.Read(dval));
                        break;
                }
            }

            var result = method.Invoke(target, args.ToArray());
            var ret = message.CreateReply();

            if (result != null)
            {
                if (result is Task tsk)
                    tsk.Wait();

                var rty = result.GetType();

                if (rty != typeof(Task))
                {
                    if (rty.IsGenericType && rty.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var resultInvk = rty.GetProperty(nameof(Task<object>.Result), BindingFlags.Public | BindingFlags.Instance)!.GetGetMethod()!;
                        result = resultInvk.Invoke(result, []);

                        rty = rty.GetGenericArguments().First();
                    }

                    var dval = IDBusValue.GetForType(rty);
                    dval.Value = result!;
                    ret.GetBodyWriter().Write(dval);
                }
            }

            connection.QueueMessage(ret);
        }
        catch (Exception ex)
        {
            DBusConnection.LOGGER.Add($"error calling interface {name}.{member}:", LogLevel.Error, ex);
            connection.QueueMessage(message.CreateError(ex));
        }
    }

    private static (Dictionary<string, MethodInfo> methods, Dictionary<string, PropertyInfo> properties) getMembers(object target, bool all)
    {
        var mthds = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                          .Where(x => all || x.GetCustomAttribute<DBusMemberAttribute>() != null)
                          .Select<MethodInfo, (MethodInfo mth, DBusMemberAttribute? attr)>(x => (x, x.GetCustomAttribute<DBusMemberAttribute>()));

        var props = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                          .Where(x => all || x.GetCustomAttribute<DBusMemberAttribute>() != null)
                          .Select<PropertyInfo, (PropertyInfo prp, DBusMemberAttribute? attr)>(x => (x, x.GetCustomAttribute<DBusMemberAttribute>()));

        return (
            mthds.ToDictionary(x => x.attr?.CustomName ?? x.mth.Name, x => x.mth),
            props.ToDictionary(x => x.attr?.CustomName ?? x.prp.Name, x => x.prp)
        );
    }

    public void WriteIntrospect(StringWriter sw)
    {
        foreach (var (key, prop) in properties)
        {
            var dval = IDBusValue.GetForType(prop.PropertyType);
            string access;

            if (prop is { CanRead: true, CanWrite: true })
                access = "readwrite";
            else if (prop.CanRead)
                access = "read";
            else
                throw new Exception($"Property {target.GetType().FullName}.{prop.Name} needs to be readable!");

            sw.WriteLine($"    <property type=\"{dval.GetDBusSignature()}\" name=\"{key}\" access=\"{access}\" />");
        }

        foreach (var (key, method) in methods)
        {
            sw.WriteLine($"    <method name=\"{key}\">");

            foreach (var parameter in method.GetParameters())
            {
                switch (parameter.ParameterType.FullName ?? "")
                {
                    // skip
                    case "Midori.DBus.DBusMessage":
                    case "Midori.DBus.DBusConnection":
                        break;

                    default:
                        var dval = IDBusValue.GetForType(parameter.ParameterType);
                        sw.WriteLine($"      <arg type=\"{dval.GetDBusSignature()}\" name=\"{parameter.Name}\" direction=\"in\"/>");
                        break;
                }
            }

            var ret = method.ReturnType;

            if (ret != typeof(void) && ret != typeof(Task))
            {
                if (ret.IsGenericType)
                {
                    var gen = ret.GetGenericTypeDefinition();

                    if (gen == typeof(Task<>))
                        ret = ret.GenericTypeArguments.First();
                }

                var dval = IDBusValue.GetForType(ret);
                sw.WriteLine($"      <arg type=\"{dval.GetDBusSignature()}\" name=\"{ret.Name}\" direction=\"out\"/>");
            }

            sw.WriteLine("    </method>");
        }
    }

    public IDBusValue GetProperty(string member)
    {
        if (!properties.TryGetValue(member, out var info))
            throw new InvalidOperationException($"Property '{member}' does not exist.");

        var val = info.GetValue(target)!;
        return new DBusVariantValue(val);
    }

    public Dictionary<string, DBusVariantValue> GetAllProperties()
    {
        var dict = new Dictionary<string, DBusVariantValue>();

        foreach (var (key, info) in properties)
        {
            var val = info.GetValue(target)!;
            dict.Add(key, new DBusVariantValue(val));
        }

        return dict;
    }
}
