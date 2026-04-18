using Midori.Networking;
using Newtonsoft.Json;

namespace Midori.API.Components;

public sealed class APIReturn<T>
{
    [JsonProperty("status")]
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("data")]
    public T? Result { get; set; }

    internal APIReturn()
    {
    }

    public static implicit operator APIReturn<T>(T val) => new() { Result = val };
    public static implicit operator APIReturn<T>(StatusReturn val) => new() { Status = val.Status, Message = val.Message };
}

public sealed class StatusReturn
{
    public HttpStatusCode Status { get; }
    public string Message { get; }

    public StatusReturn(HttpStatusCode status, string message)
    {
        Status = status;
        Message = message;
    }
}

public static class Returns
{
    public static StatusReturn Message(HttpStatusCode status, string message) => new(status, message);
    public static StatusReturn NotFound() => new(HttpStatusCode.NotFound, "The requested object could not be found.");
}
