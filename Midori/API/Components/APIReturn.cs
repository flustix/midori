using System.ComponentModel.DataAnnotations;
using Midori.Networking;
using Midori.Utils;
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

    [JsonProperty("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonIgnore]
    public Exception? Exception { get; set; }

    [JsonProperty("exception", NullValueHandling = NullValueHandling.Ignore)]
    public object? SentException
    {
        get
        {
            if (Exception is null) return null;
            if (!RuntimeUtils.IsDebugBuild) return null;

            return new
            {
                message = Exception.Message,
                trace = (Exception.StackTrace ?? "").Split("\n").Select(x => x.Trim())
            };
        }
    }

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

    public static StatusReturn Okay() => Message(HttpStatusCode.OK, "OK");
    public static StatusReturn Created(string message = "") => Message(HttpStatusCode.Created, message);
    public static StatusReturn NoContent(string message = "") => Message(HttpStatusCode.NoContent, message);

    public static StatusReturn NotModified(string message = "") => Message(HttpStatusCode.NotModified, message);

    public static StatusReturn NotFound() => Message(HttpStatusCode.NotFound, "The requested object could not be found.");
    public static StatusReturn NotFound(string obj) => Message(HttpStatusCode.NotFound, $"The requested {obj} could not be found.");

    public static APIReturn<T> ValidationError<T>(List<ValidationResult> results) => new()
    {
        Status = HttpStatusCode.BadRequest,
        Message = results.FirstOrDefault()?.ErrorMessage ?? "Failed to validate request.",
        Errors = results.SelectMany(r => r.MemberNames.Select(n => new { n, r = r.ErrorMessage ?? "Unknown Error" }))
                        .GroupBy(x => x.n)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.r).ToArray()
                        )
    };
}
