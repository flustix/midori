using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Midori.Searching.Properties;

namespace Midori.Searching;

public partial class SearchFilter<T>
{
    private Expression<Func<T, bool>> expr { get; }
    private List<IPropertyFilter> filters { get; }

    public SearchFilter(string query, IEnumerable<IPropertyFilter>? extra = null)
    {
        filters = new List<IPropertyFilter>
        {
            new StringPropertyFilter(),
            new ListPropertyFilter()
        };

        if (extra is not null)
            filters.AddRange(extra);

        var tokens = tokenize(query);

        var props = new List<string>();
        var text = new List<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            if (i + 2 < tokens.Count && validOp(tokens[i + 1]))
            {
                props.Add(tokens[i]);
                props.Add(tokens[i + 1]);
                props.Add(tokens[i + 2]);
                i += 2;
            }
            else
                text.Add(tokens[i]);
        }

        var propEx = props.Count > 0 ? buildProps(props) : null;
        var textEx = text.Count > 0 ? buildText(text) : null;

        if (propEx != null && textEx != null)
            expr = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(Expression.Invoke(propEx, propEx.Parameters), Expression.Invoke(textEx, propEx.Parameters)), propEx.Parameters);
        else
            expr = propEx ?? textEx ?? (x => true);
    }

    #region Simple Text

    private Expression<Func<T, bool>> buildText(List<string> tokens)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var props = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<SearchableAttribute>() != null)
                             .Where(p => p.PropertyType == typeof(string))
                             .ToList();

        if (props.Count == 0)
            return x => false;

        Expression? finalExpr = null;

        foreach (var token in tokens)
        {
            var searchText = token.Trim('*');
            var startsWith = token.StartsWith('*');
            var endsWith = token.EndsWith('*');

            Expression? wordMatchExpr = null;
            var method = GetStringComparisonMethod(startsWith, endsWith);
            var valueExpr = Expression.Constant(searchText);

            foreach (var prop in props)
            {
                var property = Expression.Property(param, prop);
                var condition = Expression.Call(property, method, valueExpr);
                wordMatchExpr = wordMatchExpr == null ? condition : Expression.OrElse(wordMatchExpr, condition);
            }

            if (wordMatchExpr != null)
                finalExpr = finalExpr == null ? wordMatchExpr : Expression.AndAlso(finalExpr, wordMatchExpr);
        }

        return finalExpr == null ? x => false : Expression.Lambda<Func<T, bool>>(finalExpr, param);
    }

    #endregion

    #region Properties

    private Expression<Func<T, bool>> buildProps(List<string> tokens)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var props = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<SearchableAttribute>() != null)
                             .ToDictionary(x => x.GetCustomAttribute<SearchableAttribute>()!.Key.ToLowerInvariant(), x => x);

        if (props.Count == 0)
            return x => false;

        var groups = groupTokens(tokens);
        Expression? finalExpr = null;

        foreach (var (key, op, val) in groups)
        {
            var split = key.Split(".", StringSplitOptions.RemoveEmptyEntries);

            if (!props.TryGetValue(split[0].ToLowerInvariant(), out var info))
                return x => false;

            // always prefer the custom filters
            var filter = filters.LastOrDefault(f => f.IsValid(info));

            if (filter is null)
                throw new InvalidOperationException($"Type of '{key}' is an unknown type '{info.PropertyType.Name}'.");

            var part = filter.BuildFilter(param, info, split, parseOperator(op), val);
            finalExpr = finalExpr != null ? Expression.And(finalExpr, part) : part;
        }

        var result = finalExpr == null ? x => false : Expression.Lambda<Func<T, bool>>(finalExpr, param);
        return result;
    }

    private List<(string, string, string)> groupTokens(List<string> tokens)
    {
        var list = new List<(string, string, string)>();

        string key = string.Empty;
        string op = string.Empty;

        for (var i = 0; i < tokens.Count; i++)
        {
            var idx = i % 3;
            var t = tokens[i];

            switch (idx)
            {
                case 0:
                    key = t;
                    break;

                case 1:
                    op = t;
                    break;

                case 2:
                    list.Add((key, op, t));
                    break;
            }
        }

        return list;
    }

    #endregion

    private static ComparisonOperator parseOperator(string op) => op switch
    {
        "=" => ComparisonOperator.Equal,
        "!=" => ComparisonOperator.NotEqual,
        ">" => ComparisonOperator.GreaterThan,
        "<" => ComparisonOperator.LessThan,
        ">=" => ComparisonOperator.GreaterThanOrEqual,
        "<=" => ComparisonOperator.LessThanOrEqual,
        _ => throw new InvalidOperationException($"Invalid operator '{op}'.")
    };

    public static MethodInfo GetStringComparisonMethod(bool startsWith, bool endsWith)
    {
        if (startsWith && endsWith) return typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        if (startsWith) return typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;
        if (endsWith) return typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;

        return typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
    }

    public List<T> Filter(IEnumerable<T> items) => items.AsQueryable().Where(expr).ToList();

    #region Regex

    private static List<string> tokenize(string query)
    {
        var matches = tokenRegex().Matches(query);
        return matches.Select(m => m.Groups["quoted"].Success ? m.Value.Trim('"') : m.Value).ToList();
    }

    private static bool validOp(string token) => token is "=" or "!=" or ">" or "<" or ">=" or "<=";

    [GeneratedRegex("""(?<quoted>"[^"]*")|(?<expression>[=><!]+)|(?<word>\*?[^*=<>! ]+\*?)""", RegexOptions.Compiled)]
    private static partial Regex tokenRegex();

    #endregion
}
