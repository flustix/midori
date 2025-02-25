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
        var buffer = new List<byte>();

        var end = false;

        while (!end)
        {
            end = stream.ReadByte().EqualTo('\r', add)
                  && stream.ReadByte().EqualTo('\n', add)
                  && stream.ReadByte().EqualTo('\r', add)
                  && stream.ReadByte().EqualTo('\n', add);
        }

        var bytes = buffer.ToArray();

        return Encoding.UTF8.GetString(bytes)
                       .Replace(HttpBase.CR_LF_SP, " ")
                       .Replace(HttpBase.CR_LF_TB, " ")
                       .Split(HttpBase.CR_LF, StringSplitOptions.RemoveEmptyEntries);

        void add(int v)
        {
            if (v == -1)
                throw new EndOfStreamException("Header data finished unexpectedly.");

            buffer.Add((byte)v);
        }
    }
}
