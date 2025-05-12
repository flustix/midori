namespace Midori.Utils;

public static class TypeHelper
{
    private static readonly Dictionary<string, Type> type_map = new();

    public static Type? FindType(string str)
    {
        if (type_map.TryGetValue(str, out var type))
            return type;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(x =>
        {
            try
            {
                return x.GetTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        });

        type = types.FirstOrDefault(x => x.FullName == str);

        if (type is null)
            return null;

        type_map[str] = type;
        return type;
    }
}
