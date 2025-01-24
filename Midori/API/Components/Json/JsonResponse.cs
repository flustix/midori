using Midori.Networking;
using Newtonsoft.Json;

namespace Midori.API.Components.Json;

public class JsonResponse
{
    [JsonProperty("status")]
    public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

    [JsonProperty("message")]
    public string Message { get; init; } = "OK";

    [JsonProperty("pagination")]
    public JsonPagination? Pagination { get; set; }

    [JsonProperty("data")]
    public object? Data { get; init; } = new();
}
