using Midori.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Midori.Networking.WebSockets.Typed;

[JsonObject]
internal class TypedInvokeRequest
{
    [JsonProperty("id")]
    public string InvokeID { get; set; } = string.Empty;

    [JsonProperty("type")]
    public InvokeType Type { get; set; } = InvokeType.Invoke;

    [JsonProperty("method")]
    public string MethodName { get; set; } = string.Empty;

    [JsonProperty("args")]
    public JToken?[] Arguments { get; set; } = Array.Empty<JToken>();

    private TypedInvokeRequest()
    {
    }

    public static TypedInvokeRequest Create(string method, object?[] args) => new()
    {
        InvokeID = RandomizeUtils.GenerateRandomString(32, CharacterType.AllOfIt),
        MethodName = method,
        Arguments = args.Select(a => a is null ? null : JToken.FromObject(a)).ToArray()
    };

    internal static object?[] BuildArgsList(Type[] parameterTypes, JToken?[] arguments)
    {
        var args = new List<object?>();

        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (i + 1 > arguments.Length)
                throw new ArgumentException("not enough args");

            var parameter = parameterTypes[i];
            var token = arguments[i];

            if (token is null)
            {
                args.Add(null);
                continue;
            }

            args.Add(token.ToObject(parameter));
        }

        return args.ToArray();
    }

    internal enum InvokeType
    {
        Invoke,
        Return
    }
}
