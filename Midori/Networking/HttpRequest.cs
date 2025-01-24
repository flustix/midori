namespace Midori.Networking;

public class HttpRequest : HttpBase
{
    internal string StatusLine => $"{Method} {Target} HTTP/{Version}{CR_LF}";

    protected override string MessageHeader => StatusLine + HeaderSection;

    public string Method { get; }
    public string Target { get; }
    public string Version { get; }

    public Dictionary<string, string> QueryParameters { get; init; } = new();

    internal HttpRequest(string method, string target, string version, HttpHeaderCollection headers)
        : base(headers)
    {
        Method = method.ToUpperInvariant();
        Target = target.ToLowerInvariant();
        Version = version;
    }

    internal static HttpRequest ReadRequest(Stream stream)
    {
        return HttpParser.Read(stream, parse);
    }

    private static HttpRequest parse(string[] headers)
    {
        var len = headers.Length;

        if (len == 0)
            throw new ArgumentException("Request headers are empty.");

        var rql = headers[0].Split(" ", 3);

        if (rql.Length != 3)
            throw new ArgumentException("Request line is invalid.");

        var method = rql[0];
        var target = rql[1];
        var version = rql[2][5..];

        var collection = new HttpHeaderCollection();

        for (var i = 1; i < len; i++)
            collection.AddLine(headers[i]);

        var tSplit = target.Split("?");
        var query = new Dictionary<string, string>();

        if (tSplit.Length > 1)
            query = HttpParser.ParseQueryString(tSplit[1]);

        return new HttpRequest(method, tSplit[0], version, collection) { QueryParameters = query };
    }
}
