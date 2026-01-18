using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MethodArgument = (string name, string type, bool ret);
using InterfaceProperty = (string name, string type, string access);
using InterfaceSignalArgument = (string n, string t);
using InterfaceSignal = (string name, (string n, string t)[] args);

namespace Midori.DBus.SourceGen;

[Generator]
public class IntrospectGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var existing = new List<string>();

        var files = context.AdditionalTextsProvider
                           .Where(static file => file.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                           .SelectMany((file, ct) =>
                           {
                               var text = file.GetText(ct);
                               if (text is null) return new List<DBusInterface>();

                               var str = text.ToString();
                               var doc = XDocument.Parse(str);
                               return parseDoc(doc);
                           }).Select((x, _) =>
                           {
                               if (existing.Contains(x.Name))
                                   return null;

                               existing.Add(x.Name);
                               return x;
                           }).Where(x => x != null);

        context.RegisterSourceOutput(files, (s, t) => s.AddSource($"{t!.Name}", SourceText.From(t.Content, Encoding.UTF8)));
    }

    private static List<DBusInterface> parseDoc(XDocument doc)
    {
        var list = new List<DBusInterface>();

        var node = doc.Elements().FirstOrDefault(x => x.Name.LocalName == "node");
        if (node is null) return [];

        foreach (var xInterface in node.Elements("interface"))
        {
            var xName = xInterface.Attribute("name")?.Value;
            if (xName is null) continue;

            var lastDot = xName.LastIndexOf('.');
            var nspace = xName[..lastDot];
            var name = xName[(lastDot + 1)..];

            var sb = new StringBuilder();
            sb.AppendLine($"namespace {nspace};");
            sb.AppendLine();
            sb.AppendLine($"[Midori.DBus.Attributes.DBusInterface(\"{xName}\")]");
            sb.AppendLine($"public interface I{name} : Midori.DBus.Impl.IDBusWatchable");
            sb.AppendLine("{");

            foreach (var xMethod in xInterface.Elements("method"))
            {
                var xMethodName = xMethod.Attribute("name")?.Value;
                if (xMethodName is null) continue;

                var args = xMethod.Elements("arg").Select<XElement, MethodArgument?>(x =>
                {
                    var xArgName = x.Attribute("name")?.Value;
                    var xArgType = x.Attribute("type")?.Value;
                    if (xArgName is null || xArgType is null) return null;

                    return (xArgName, getType(xArgType), x.Attribute("direction")?.Value == "out");
                }).OfType<MethodArgument>().ToList();

                var ret = "System.Threading.Tasks.Task";

                if (args.Any(x => x.ret))
                    ret = $"System.Threading.Tasks.Task<{args.First(x => x.ret).type}>";

                sb.AppendLine($"    {ret} {xMethodName}({string.Join(", ", args.Where(x => !x.ret).Select(x => $"{x.type} {x.name}"))});");
            }

            var signals = xInterface.Elements("signal").Select<XElement, InterfaceSignal?>(x =>
            {
                var xSignalName = x.Attribute("name")?.Value;
                if (xSignalName is null) return null;

                var args = x.Elements("arg").Select<XElement, InterfaceSignalArgument?>(a =>
                {
                    var xArgName = a.Attribute("name")?.Value ?? "";
                    var xArgType = a.Attribute("type")?.Value;
                    if (xArgType is null) return null;

                    return (xArgName, getType(xArgType));
                }).OfType<InterfaceSignalArgument>().ToList();

                return (xSignalName, args.ToArray());
            }).OfType<InterfaceSignal>().ToList();

            var props = xInterface.Elements("property").Select<XElement, InterfaceProperty?>(x =>
            {
                var xPropName = x.Attribute("name")?.Value;
                var xPropType = x.Attribute("type")?.Value;
                var xPropAccess = x.Attribute("access")?.Value ?? "read";
                if (xPropName is null || xPropType is null) return null;

                return (xPropName, xPropType, xPropAccess);
            }).OfType<InterfaceProperty>().ToList();

            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"public static class {name}Extensions");
            sb.AppendLine("{");

            foreach (var (n, args) in signals)
            {
                var t = args.Length switch
                {
                    0 => "",
                    1 => $"<{args.First().t}>",
                    _ => $"<({string.Join(", ", args.Select(x => x.t))})>",
                };

                if (args.Length == 0)
                    continue; // TODO: addmatch doesnt support 0 types

                sb.AppendLine($"    public static System.IDisposable Listen{n}(this I{name} o, System.Action{t} act) => o.ListenToSignal(\"{n}\", act);");
            }

            if (signals.Any())
                sb.AppendLine();

            var first = true;

            foreach (var (n, dt, _) in props)
            {
                if (!first)
                    sb.AppendLine();

                var t = getType(dt);

                sb.AppendLine($"    public static {t} Get{n}(this I{name} o) => o.GetPropertyValue<{t}>(\"{n}\");");
                sb.AppendLine($"    public static void Watch{n}(this I{name} o, System.Action<{t}> act) => o.StartWatching(\"{n}\", act);");
                first = false;
            }

            sb.AppendLine("}");

            list.Add(new DBusInterface(xName, sb.ToString()));
        }

        return list;
    }

    private static string getType(string sig) => sig[0] switch
    {
        'y' => "byte",
        'b' => "bool",
        'n' => "short",
        'q' => "ushort",
        'i' => "int",
        'u' => "uint",
        'x' => "long",
        't' => "ulong",
        'd' => "double",
        's' => "string",
        'o' => "Midori.DBus.DBusObjectPath",
        'v' => "Midori.DBus.Values.DBusVariantValue",
        'a' => sig[1] switch
        {
            '{' => $"System.Collections.Generic.Dictionary<{getType(sig[2].ToString())},{getType(sig[3..^1])}>",
            _ => $"System.Collections.Generic.List<{getType(sig[1..])}>"
        },
        _ => $"/*{sig}*/object"
    };
}
