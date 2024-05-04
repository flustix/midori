namespace Midori.API;

public static class DefaultResponseStrings
{
    public static string MissingHeader(string header) => $"The '{header}' header is missing.";

    public static string MissingQuery(string name) => $"The '{name}' query parameter is missing.";
    public static string InvalidQuery(string parameter, string type) => $"The parameter '{parameter}' is not a valid {type}.";

    public static string InvalidParameter(string parameter, string type) => $"The parameter '{parameter}' is not a valid {type}.";
}
