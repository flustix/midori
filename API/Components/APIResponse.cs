using System.Net;
using Newtonsoft.Json;

namespace Midori.API.Components;

public class APIResponse
{
    [JsonProperty("status")]
    public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

    [JsonProperty("message")]
    public string Message { get; init; } = "OK";

    [JsonProperty("pagination")]
    public APIPagination? Pagination { get; set; }

    [JsonProperty("data")]
    public object? Data { get; init; } = new();
}
