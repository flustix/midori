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

    public Stream InputStream
    {
        get
        {
            var ms = new MemoryStream(MessageBody);
            return ms;
        }
    }

    internal HttpBase(HttpHeaderCollection headers)
    {
        Headers = headers;
    }

    public byte[] ToByteArray()
    {
        var header = Encoding.UTF8.GetBytes(MessageHeader);
        var body = MessageBody;

        var result = new byte[header.Length + body.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(body, 0, result, header.Length, body.Length);

        return result;
    }

    public void WriteToStream(Stream stream)
    {
        var buffer = ToByteArray();
        stream.Write(buffer, 0, buffer.Length);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        Headers.Clear();
        MessageBody = Array.Empty<byte>();
    }
}
