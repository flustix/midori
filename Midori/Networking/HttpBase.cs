using System.Text;

namespace Midori.Networking;

public abstract class HttpBase : IDisposable
{
    internal const string CR_LF = "\r\n";
    internal const string CR_LF_TB = "\r\n\t";
    internal const string CR_LF_SP = "\r\n ";

    protected abstract string MessageHeader { get; }

    public HttpHeaderCollection Headers { get; }

    protected string HeaderSection
    {
        get
        {
            var buffer = new StringBuilder();

            foreach (var key in Headers.AllKeys)
                buffer.Append($"{key}: {Headers[key]}{CR_LF}");

            buffer.Append(CR_LF);
            return buffer.ToString();
        }
    }

    public string ContentType
    {
        get => Headers["Content-Type"] ?? string.Empty;
        set => Headers["Content-Type"] = value;
    }

    public long ContentLength
    {
        get => long.TryParse(Headers["Content-Length"] ?? "0", out var result) ? result : 0;
        set => Headers["Content-Length"] = value.ToString();
    }

    internal byte[] MessageBody { get; set; } = Array.Empty<byte>();

    public virtual Stream BodyStream
    {
        get
        {
            var ms = new MemoryStream(MessageBody);
            return ms;
        }
        set => throw new NotImplementedException();
    }

    internal HttpBase(HttpHeaderCollection headers)
    {
        Headers = headers;
    }

    public async Task WriteToStream(Stream stream)
    {
        var header = Encoding.UTF8.GetBytes(MessageHeader);
        await stream.WriteAsync(header);

        if (BodyStream.CanSeek)
            BodyStream.Seek(0, SeekOrigin.Begin);

        await BodyStream.CopyToAsync(stream);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        Headers.Clear();
        MessageBody = Array.Empty<byte>();
        BodyStream.Dispose();
    }
}
