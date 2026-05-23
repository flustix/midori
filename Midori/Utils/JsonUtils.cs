using Midori.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Midori.Utils;

public static class JsonUtils
{
    public const string JSON_CONSTRUCTOR_ERROR = "This constructor is for json parsing only.";

    public static List<JsonConverter> Converters { get; } = new();

    /// <summary>
    /// Creates a copy of a JObject.
    /// </summary>
    public static JObject Copy(this JObject obj) => JObject.Parse(obj.ToString());

    /// <summary>
    /// Creates a copy of an object by serializing and then deserializing it.
    /// </summary>
    /// <param name="obj">The object to copy.</param>
    /// <returns>The created copy.</returns>
    public static T? JsonCopy<T>(this T obj) => obj.Serialize().Deserialize<T>();

    public static T? Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json, globalSettings());
    public static object? Deserialize(this string json, Type type) => JsonConvert.DeserializeObject(json, type, globalSettings());
    public static string Serialize<T>(this T obj, bool indent = false) => JsonConvert.SerializeObject(obj, globalSettings(indent));

    public static bool TryDeserialize<T>(this string json, out T? result)
    {
        try
        {
            result = json.Deserialize<T>();
            return result is not null;
        }
        catch (JsonException ex)
        {
            result = default;
            Logger.Error(ex, "Failed to parse JSON!");
            return false;
        }
    }

    private static JsonSerializerSettings globalSettings(bool indent = false) => new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = indent ? Formatting.Indented : Formatting.None,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = Converters
    };
}
