using Midori.Utils.Extensions;

namespace Midori.Networking;

public class HttpResponse : HttpBase
{
    protected override string MessageHeader => $"HTTP/1.1 {(int)StatusCode} {StatusCode.GetHttpReason()}{CR_LF}" + HeaderSection;

    public Stream OutputStream { get; } = new MemoryStream();

    public HttpStatusCode StatusCode { get; set; }

    private HttpResponse(HttpHeaderCollection headers)
        : base(headers)
    {
    }

    public HttpResponse(HttpStatusCode code)
        : this(new HttpHeaderCollection())
    {
        StatusCode = code;
    }

    internal HttpResponse(HttpStatusCode code, HttpHeaderCollection headers)
        : base(headers)
    {
        StatusCode = code;
    }

    internal static HttpResponse ReadResponse(Stream stream)
    {
        return HttpParser.Read(stream, parse);
    }

    private static HttpResponse parse(string[] headers)
    {
        var len = headers.Length;

        if (len == 0)
            throw new ArgumentException("Response headers are empty.");

        var rql = headers[0].Split(" ", 3);

        if (rql.Length != 3)
            throw new ArgumentException("Response line is invalid.");

        var version = rql[0][5..];
        var code = (HttpStatusCode)int.Parse(rql[1]);
        var reason = rql[2];

        var collection = new HttpHeaderCollection();

        for (var i = 1; i < len; i++)
            collection.AddLine(headers[i]);

        return new HttpResponse(code, collection);
    }

    /// <summary>
    /// Writes the OutputStream to the MessageBody.
    /// </summary>
    public void Flush()
    {
        MessageBody = ((MemoryStream)OutputStream).ToArray();
    }

    public override void Dispose()
    {
        base.Dispose();

        OutputStream.Dispose();
    }
}
