using System.Linq.Expressions;
using System.Reflection;

namespace Midori.Searching;

public interface IPropertyFilter
{
    bool IsValid(PropertyInfo prop);
    Expression BuildFilter(ParameterExpression param, PropertyInfo prop, string[] key, ComparisonOperator op, string value);

    protected static Exception ThrowInvalidOperator(string key, ComparisonOperator op, PropertyInfo info) =>
        new InvalidOperationException($"{key}: Operation {op} is not valid on {info.PropertyType.Name}.");
}
