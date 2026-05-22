using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using HttpMultipartParser;
using Midori.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Midori.API.Components;

public interface IRequestBodyContent
{
    object? GetFull(Type requestedType);
    object? GetFormEntry(Type requestedType, string name);
}

public class StreamRequestBodyContent : IRequestBodyContent
{
    private readonly Stream stream;

    public StreamRequestBodyContent(Stream stream)
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

public class JsonRequestBodyContent : IRequestBodyContent
{
    private readonly JObject parsed;

    public JsonRequestBodyContent(Stream input)
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

public class MultipartRequestBodyContent : IRequestBodyContent
{
    private readonly MultipartFormDataParser parser;

    public MultipartRequestBodyContent(Stream input)
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
            return requestedType == typeof(Stream)
                ? file.Data
                : throw new InvalidOperationException("Multipart file entries currently only support returning as a Stream.");
        }

        // we might wanna make our own multipart parser at some point, because this one will always return strings
        var param = parser.Parameters.First(x => x.Name == name);

        if (param != null)
        {
            return requestedType == typeof(string)
                ? param.Data
                : throw new InvalidOperationException("Multipart parameter entries currently only support returning as a string.");
        }

        return null;
    }
}
