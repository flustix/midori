using Midori.Logging;
using Newtonsoft.Json;

namespace Midori.Utils;

public static class JsonUtils
{
    public static List<JsonConverter> Converters { get; } = new();

    public static T? Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json, globalSettings());
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
