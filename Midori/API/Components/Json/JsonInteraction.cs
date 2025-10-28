using System.Text;
using Midori.Networking;
using Midori.Utils;

namespace Midori.API.Components.Json;

public class JsonInteraction : JsonInteraction<JsonResponse>
{
}

public class JsonInteraction<T> : APIInteraction
    where T : JsonResponse, new()
{
    private JsonPagination? pagination;

    public async Task Reply(HttpStatusCode code, object? data = null, string message = "") => await ReplyJson(new T
    {
        Status = code,
        Message = message,
        Data = data
    });

    public override async Task ReplyMessage(HttpStatusCode code, string message, Exception? exception = null) => await ReplyJson(new T
    {
        Status = code,
        Message = message
    });

    public void SetPaginationInfo(long limit, long offset, long total, long count)
        => pagination = new JsonPagination(limit, offset, total, count);

    protected virtual async Task ReplyJson(T response)
    {
        response.Pagination = pagination;

        var json = response.Serialize();
        var buffer = Encoding.UTF8.GetBytes(json);
        Response.StatusCode = response.Status;
        Response.Headers.Add("Content-Type", "application/json");
        await ReplyData(buffer);
    }
}
