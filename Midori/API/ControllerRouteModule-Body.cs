using System.Reflection;
using System.Text;
using Midori.Networking;
using Midori.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Midori.API;

internal partial class ControllerRouteModule<T>
{
    private IBodyContent? body;

    private IBodyContent getBody(HttpServerContext ctx)
    {
        if (body != null)
            return body;

        try
        {
            var stream = ctx.Request.BodyStream;

            body = ctx.Request.Headers["Content-Type"] switch
            {
                "application/json" => new JsonBodyContent(stream),
                _ => new StreamBodyContent(stream)
            };

            return body;
        }
        catch
        {
            throw new KeyNotFoundException("Invalid request body.");
        }
    }

    private interface IBodyContent
    {
        object? GetFull(Type requestedType);
        object? GetFormEntry(Type requestedType, string name);
    }

    private class StreamBodyContent : IBodyContent
    {
        private readonly Stream stream;

        public StreamBodyContent(Stream stream)
        {
            this.stream = stream;
        }

        public object? GetFull(Type requestedType)
        {
            if (requestedType != typeof(Stream))
                throw new KeyNotFoundException($"Invalid request body.");

            return stream;
        }

        public object? GetFormEntry(Type requestedType, string name) => throw new KeyNotFoundException("Invalid request body.");
    }

    private class JsonBodyContent : IBodyContent
    {
        private readonly JObject parsed;

        public JsonBodyContent(Stream input)
        {
            var bytes = new byte[input.Length];
            input.ReadExactly(bytes);

            var text = Encoding.UTF8.GetString(bytes);
            parsed = text.Deserialize<JObject>() ?? throw new JsonException("Failed to parse body as json.");
        }

        public object? GetFull(Type requestedType)
        {
            var stream = requestedType == typeof(Stream);
            var bytes = requestedType == typeof(byte[]);
            var str = requestedType == typeof(string);

            var json = parsed.Serialize();
            if (str) return json;

            if (stream || bytes)
            {
                var by = Encoding.UTF8.GetBytes(json);
                if (bytes) return by;

                return new MemoryStream(by);
            }

            var method = typeof(JsonUtils).GetMethod(nameof(JsonUtils.Deserialize), BindingFlags.Public | BindingFlags.Static)!
                                          .MakeGenericMethod(requestedType);

            return method.Invoke(null, new object?[] { json });
        }

        public object? GetFormEntry(Type requestedType, string name)
        {
            var val = parsed[name];
            return val?.ToObject(requestedType);
        }
    }
}
