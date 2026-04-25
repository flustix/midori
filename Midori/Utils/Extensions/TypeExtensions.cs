using System.Reflection;
using Midori.API.Attributes;
using Midori.Logging;

namespace Midori.Utils.Extensions;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, List<(MethodInfo, HttpRouteAttribute, string)>> controller_methods = new();

    public static bool IsNullable(this ParameterInfo parameter)
    {
        if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
            return true;

        var ctx = new NullabilityInfoContext().Create(parameter);
        return ctx.WriteState == NullabilityState.Nullable || ctx.ReadState == NullabilityState.Nullable;
    }

    public static List<(MethodInfo method, HttpRouteAttribute attr, string path)> GetControllerMethods(this Type type)
    {
        if (controller_methods.TryGetValue(type, out var cache))
            return cache;

        var prefix = string.Empty;
        var control = type.GetCustomAttribute<ControllerAttribute>();
        if (control != null) prefix = control.Prefix;

        if (string.IsNullOrWhiteSpace(prefix))
        {
            Logger.Log($"{type.FullName} is missing a {nameof(ControllerAttribute)}. Prefix is defaulting to /.", LoggingTarget.Network);
            prefix = "/";
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                          .Where(x => x.GetCustomAttribute<HttpRouteAttribute>() != null)
                          .ToList();

        var result = methods.Select(method =>
        {
            var attr = method.GetCustomAttribute<HttpRouteAttribute>()!;
            var path = Path.Combine(prefix, attr.Path.TrimStart('/')).Replace("\\", "/");
            return (method, attr, path);
        }).ToList();

        controller_methods[type] = result;

        return result;
    }
}
