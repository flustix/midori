using System.Text;
using System.Web;
using Midori.Utils.Extensions;

namespace Midori.Networking;

internal static class HttpParser
{
    internal static Dictionary<string, string> ParseQueryString(string query)
    {
        var dict = new Dictionary<string, string>();
        var aSplit = query.Split("&");

        foreach (var kv in aSplit)
        {
            var split = kv.Split("=");
            var key = split[0];
            var value = split.Length > 1 ? split[1] : "";

            dict.Add(key, HttpUtility.UrlDecode(value));
        }

        return dict;
    }

    internal static T Read<T>(Stream stream, Func<string[], T> parser) where T : HttpBase
    {
        var headers = readHeaders(stream);
        var ret = parser(headers);

        if (ret.ContentLength > 0)
        {
            var body = stream.ReadBytes(ret.ContentLength);
            ret.BodyStream.Write(body);

            if (ret.BodyStream.CanSeek)
                ret.BodyStream.Seek(0, SeekOrigin.Begin);
        }

        return ret;
    }

    private static string[] readHeaders(Stream stream)
    {
        var buffer = new List<byte>(1024);
        var state = 0;

        while (state < 4)
        {
            var b = stream.ReadByte();
            add(b);

            // header end match: \r\n\r\n
            state = state switch
            {
                0 or 2 when b == '\r' => state + 1,
                1 or 3 when b == '\n' => state + 1,
                _ when b == '\r' => 1,
                _ => 0
            };
        }

        var rawHeaders = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count - 4);
        var headers = new List<string>();

        using var reader = new StringReader(rawHeaders);

        while (reader.ReadLine() is { } line)
        {
            // check for folds
            if (line.Length > 0 && (line[0] == ' ' || line[0] == '\t') && headers.Count > 0)
                headers[^1] += " " + line.TrimStart();
            else
                headers.Add(line);
        }

        return headers.ToArray();

        void add(int v)
        {
            if (v == -1)
                throw new EndOfStreamException("Header data finished unexpectedly.");

            buffer.Add((byte)v);
        }
    }
}
