using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using HttpMultipartParser;
using Midori.API.Components;
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
            var ct = ctx.Request.Headers["Content-Type"] ?? "";

            if (ct.StartsWith("application/json"))
                body = new JsonBodyContent(stream);
            else if (ct.StartsWith("multipart/form-data"))
                body = new MultipartBodyContent(stream);
            else
                body = new StreamBodyContent(stream);

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

            var result = method.Invoke(null, new object?[] { json });
            if (result is null) return null;

            var results = new List<ValidationResult>();
            var success = Validator.TryValidateObject(result, new ValidationContext(result), results, true);
            return success ? result : throw new RequestValidationException(results);
        }

        public object? GetFormEntry(Type requestedType, string name)
        {
            var val = parsed[name];
            return val?.ToObject(requestedType);
        }
    }

    private class MultipartBodyContent : IBodyContent
    {
        private readonly MultipartFormDataParser parser;

        public MultipartBodyContent(Stream input)
        {
            parser = MultipartFormDataParser.Parse(input, Encoding.UTF8);
        }

        public object? GetFull(Type requestedType)
        {
            throw new InvalidOperationException("Cannot get a full multipart as a parsed type.");
        }

        public object? GetFormEntry(Type requestedType, string name)
        {
            var file = parser.Files.FirstOrDefault(x => x.Name == name);

            if (file != null)
            {
                if (requestedType == typeof(Stream))
                    return file.Data;

                throw new InvalidOperationException("Multipart entries currently only support returning as a Stream.");
            }

            // TODO: fallback to parameters
            return null;
        }
    }
}
