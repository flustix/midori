using System.Reflection;
using Midori.DBus.Attributes;
using Midori.DBus.Exceptions;
using Midori.DBus.Values;
using Midori.Logging;

namespace Midori.DBus.Methods;

public interface IDBusPathHandler
{
    void RegisterInterface<T>(T inst)
        where T : class;

    void Handle(DBusMessage message);
}

internal class DBusPathHandler : IDBusPathHandler
{
    private readonly DBusConnection connection;
    private readonly DBusObjectPath path;
    private readonly Dictionary<string, IDBusInterfaceHandler> interfaces = new();

    public DBusPathHandler(DBusConnection connection, DBusObjectPath path)
    {
        this.connection = connection;
        this.path = path;
    }

    public void RegisterInterface<T>(T inst)
        where T : class
    {
        var type = typeof(T);
        var attr = type.GetCustomAttribute<DBusInterfaceAttribute>();
        if (attr is null) throw new InvalidOperationException($"Type {type} is missing a {nameof(DBusInterfaceAttribute)}.");

        if (interfaces.ContainsKey(attr.Interface))
            throw new InvalidOperationException("Interface is already registered to this path.");

        interfaces[attr.Interface] = new DBusInterfaceHandler(connection, attr.Interface, attr.AllAreMembers, inst);
    }

    public void Handle(DBusMessage message)
    {
        var intf = message.Interface;

        switch (intf)
        {
            case "org.freedesktop.DBus.Introspectable":
            {
                writeIntrospect(message);
                return;
            }

            case "org.freedesktop.DBus.Properties":
            {
                var body = message.GetBodyReader();
                var targetInterface = body.ReadString();

                if (!interfaces.TryGetValue(targetInterface, out var target))
                {
                    connection.SendMessage(message.CreateError(new DBusException("Interface does not exist on path.")));
                    return;
                }

                var ret = message.CreateReply();
                var writer = ret.GetBodyWriter();

                switch (message.Member)
                {
                    case "Get":
                    {
                        var member = body.ReadString();
                        var prop = target.GetProperty(member);
                        writer.Write(prop);
                        break;
                    }

                    case "GetAll":
                    {
                        var props = target.GetAllProperties();
                        var val = new DBusDictionaryValue<string, DBusVariantValue> { Value = props };
                        writer.Write(val);
                        break;
                    }
                }

                connection.SendMessage(ret);
                return;
            }

            default:
                DBusConnection.LOGGER.Add($"{intf} isn't handled by library, passing on.", LogLevel.Debug);
                break;
        }

        if (!interfaces.TryGetValue(intf, out var handler))
        {
            connection.SendMessage(message.CreateError(new DBusException("Interface does not exist on path.")));
            return;
        }

        handler.Handle(message);
    }

    private void writeIntrospect(DBusMessage message)
    {
        using var sw = new StringWriter();
        sw.WriteLine("<!DOCTYPE node PUBLIC \"-//freedesktop//DTD D-BUS Object Introspection 1.0//EN\" \"http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd\">");
        sw.WriteLine($"<node name=\"{path}\">");

        foreach (var (key, intf) in interfaces)
        {
            sw.WriteLine($"  <interface name=\"{key}\">");
            intf.WriteIntrospect(sw);
            sw.WriteLine("  </interface>");
        }

        var children = connection.CallPaths.Where(x => x.Key.StartsWith(path) && !x.Key.Equals(path)).ToDictionary();

        foreach (var (key, _) in children)
            sw.WriteLine($"  <node name=\"{key}\" />");

        sw.WriteLine("</node>");

        var str = sw.ToString();
        var ret = message.CreateReply();
        ret.GetBodyWriter().WriteString(str);
        connection.SendMessage(ret);
    }
}
