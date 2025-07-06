using System.Linq.Expressions;
using System.Reflection;

namespace Midori.Searching.Properties;

public class ListPropertyFilter : IPropertyFilter
{
    public bool IsValid(PropertyInfo prop) => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>);

    public Expression BuildFilter(ParameterExpression param, PropertyInfo prop, string[] key, ComparisonOperator op, string value)
    {
        if (key.Length < 2)
            throw new InvalidOperationException($"{key}: Missing sub-key for list.");

        var sub = key[1];

        switch (sub)
        {
            case "count":
                var parsed = int.Parse(value);
                var valueEx = Expression.Constant(parsed);
                var property = Expression.Property(Expression.Property(param, prop), nameof(List<object>.Count));

                return op switch
                {
                    ComparisonOperator.Equal => Expression.Equal(property, valueEx),
                    ComparisonOperator.NotEqual => Expression.NotEqual(property, valueEx),
                    ComparisonOperator.GreaterThan => Expression.GreaterThan(property, valueEx),
                    ComparisonOperator.LessThan => Expression.LessThan(property, valueEx),
                    ComparisonOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, valueEx),
                    ComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, valueEx),
                    _ => throw IPropertyFilter.ThrowInvalidOperator(key.First(), op, prop)
                };

            default:
                throw new InvalidOperationException($"{key}: Invalid sub-key '{sub}'.");
        }
    }
}
