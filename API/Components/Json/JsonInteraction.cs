using System.Net;
using System.Text;
using Midori.Utils;

namespace Midori.API.Components.Json;

public class JsonInteraction : APIInteraction
{
    private JsonPagination? pagination;

    public async Task Reply(HttpStatusCode code, object? data = null) => await reply(new JsonResponse
    {
        Status = code,
        Data = data
    });

    public async Task ReplyMessage(string message) => await reply(new JsonResponse
    {
        Message = message
    });

    public override async Task ReplyError(HttpStatusCode code, string error, Exception? exception = null) => await reply(new JsonResponse
    {
        Status = code,
        Message = error
    });

    public void SetPaginationInfo(long limit, long offset, long total, long count)
        => pagination = new JsonPagination(limit, offset, total, count);

    private async Task reply(JsonResponse response)
    {
        response.Pagination = pagination;

        var json = response.Serialize();
        var buffer = Encoding.UTF8.GetBytes(json);
        Response.StatusCode = (int)response.Status;
        Response.AddHeader("Content-Type", "application/json");
        await ReplyData(buffer);
    }
}
