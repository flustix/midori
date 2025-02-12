using System.Reflection;

namespace Midori.Utils.Extensions;

public static class TypeExtensions
{
    public static bool IsNullable(this ParameterInfo parameter)
    {
        if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
            return true;

        var ctx = new NullabilityInfoContext().Create(parameter);
        return ctx.WriteState == NullabilityState.Nullable || ctx.ReadState == NullabilityState.Nullable;
    }
}
