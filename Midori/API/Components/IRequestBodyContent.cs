using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;
using HttpMultipartParser;
using JetBrains.Annotations;
using Midori.Networking;
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

    public StreamRequestBodyContent(HttpServerContext ctx)
    {
        stream = ctx.Request.BodyStream;
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

    [UsedImplicitly]
    public JsonRequestBodyContent(HttpServerContext ctx)
    {
        var input = ctx.Request.BodyStream;
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

        var result = json.Deserialize(requestedType);
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

    [UsedImplicitly]
    public MultipartRequestBodyContent(HttpServerContext ctx)
    {
        parser = MultipartFormDataParser.Parse(ctx.Request.BodyStream, Encoding.UTF8);
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

public class FormUrlEncodedRequestBodyContent : IRequestBodyContent
{
    private readonly Dictionary<string, string> parameters;

    [UsedImplicitly]
    public FormUrlEncodedRequestBodyContent(HttpServerContext ctx)
    {
        var input = ctx.Request.BodyStream;
        var bytes = new byte[input.Length];
        input.ReadExactly(bytes);

        var text = Encoding.UTF8.GetString(bytes);

        try
        {
            parameters = Parse(text);
        }
        catch
        {
            parameters = new Dictionary<string, string>();
        }
    }

    internal FormUrlEncodedRequestBodyContent(Dictionary<string, string> dict)
    {
        parameters = dict;
    }

    public object? GetFull(Type requestedType)
    {
        var json = parameters.Serialize();
        var result = json.Deserialize(requestedType);
        if (result is null) return null;

        var results = new List<ValidationResult>();
        var success = Validator.TryValidateObject(result, new ValidationContext(result), results, true);
        return success ? result : throw new RequestValidationException(results);
    }

    public object? GetFormEntry(Type requestedType, string name)
    {
        if (!parameters.TryGetValue(name, out var val))
            return null;

        if (requestedType == typeof(string))
            return val;

        // now this might seem stupid, but it works well enough.
        // and it should even handle JSON object properly (if someone is stupid enough to do that)
        var j = JToken.Parse(val);
        return j.ToObject(requestedType);
    }

    public static Dictionary<string, string> Parse(string input)
    {
        var dict = new Dictionary<string, string>();
        var parameters = input.Split("&");

        foreach (var se in parameters)
        {
            var split = se.Split("=");
            if (split.Length != 2) throw new InvalidOperationException("");

            var key = split.First();
            var value = split.Last();
            dict[key] = HttpUtility.UrlDecode(value);
        }

        return dict;
    }
}
