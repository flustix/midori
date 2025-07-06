using System.Linq.Expressions;
using System.Reflection;

namespace Midori.Searching.Properties;

internal class StringPropertyFilter : IPropertyFilter
{
    public bool IsValid(PropertyInfo prop) => prop.PropertyType == typeof(string);

    public Expression BuildFilter(ParameterExpression param, PropertyInfo prop, string[] key, ComparisonOperator op, string value)
    {
        var searchText = value.Trim('*');
        var method = SearchFilter<dynamic>.GetStringComparisonMethod(value.StartsWith('*'), value.EndsWith('*'));

        var valueExpr = Expression.Constant(searchText);
        var property = Expression.Property(param, prop);
        Expression condition = Expression.Call(property, method, valueExpr);

        switch (op)
        {
            case ComparisonOperator.Equal:
                break;

            case ComparisonOperator.NotEqual:
                condition = Expression.Not(condition);
                break;

            default:
                throw IPropertyFilter.ThrowInvalidOperator(key.First(), op, prop);
        }

        return condition;
    }
}
