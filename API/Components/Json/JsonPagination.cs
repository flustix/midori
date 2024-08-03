using Newtonsoft.Json;

namespace Midori.API.Components.Json;

public class JsonPagination
{
    [JsonProperty("limit")]
    public long Limit { get; init; }

    [JsonProperty("offset")]
    public long Offset { get; init; }

    [JsonProperty("total")]
    public long Total { get; init; }

    [JsonProperty("count")]
    public long Count { get; init; }

    public JsonPagination(long limit, long offset, long total, long count)
    {
        Limit = limit;
        Offset = offset;
        Total = total;
        Count = count;
    }
}
