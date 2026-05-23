using System.Reflection;

namespace Midori.Utils.Extensions;

public static class ObjectExtensions
{
    public static T NullWhereSame<T>(this T copy, T orig)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            var c = prop.GetValue(copy);
            var o = prop.GetValue(orig);

            if (c?.Equals(o) ?? false)
                prop.SetValue(copy, null);
        }

        return copy;
    }
}
