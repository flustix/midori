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

    public async Task Reply(HttpStatusCode code, object? data = null) => await ReplyJson(new T
    {
        Status = code,
        Data = data
    });

    public async Task ReplyMessage(string message) => await ReplyJson(new T
    {
        Message = message
    });

    public override async Task ReplyError(HttpStatusCode code, string error, Exception? exception = null) => await ReplyJson(new T
    {
        Status = code,
        Message = error
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
