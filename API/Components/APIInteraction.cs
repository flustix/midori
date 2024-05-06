using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using HttpMultipartParser;
using Midori.Logging;
using Midori.Utils;

namespace Midori.API.Components;

public class APIInteraction
{
    protected virtual string[] AllowedMethods => new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    protected virtual string[] AllowedHeaders => new[] { "Content-Type", "Authorization", "X-Requested-With", "X-Forwarded-For" };

    protected Logger Logger { get; } = Logger.GetLogger("API");

    public HttpListenerRequest Request { get; private set; } = null!;
    public HttpListenerResponse Response { get; private set; } = null!;
    public Dictionary<string, string> Parameters { get; private set; } = null!;
    public IPAddress RemoteIP { get; private set; } = null!;

    private MultipartFormDataParser? parser;

    private APIPagination? pagination;

    public void Populate(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        Request = req;
        Response = res;
        Parameters = parameters;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Request.InputStream != null && Request.InputStream != Stream.Null && (Request.ContentType?.StartsWith("multipart/form-data") ?? false))
            parser = MultipartFormDataParser.Parse(Request.InputStream, Request.ContentEncoding);

        var forward = Request.Headers.Get("X-Forwarded-For");
        RemoteIP = string.IsNullOrEmpty(forward) ? Request.RemoteEndPoint.Address : IPAddress.Parse(forward.Split(",").First());

        OnPopulate();
    }

    /// <summary>
    /// Called after the interaction has been populated. Use this to set up any additional data like the current user.
    /// </summary>
    protected virtual void OnPopulate()
    {
    }

    public bool TryGetFile(string file, [NotNullWhen(true)] out FilePart? data)
    {
        if (parser == null)
        {
            data = null!;
            return false;
        }

        data = parser.Files.FirstOrDefault(f => f.Name == file);
        return data != null;
    }

    #region Route Parameters

    private bool tryGetParameter(string name, out string value)
    {
        var success = Parameters.TryGetValue(name, out value!);

        if (!success)
            value = "";

        return success;
    }

    public bool TryGetStringParameter(string name, out string value)
    {
        if (tryGetParameter(name, out value))
            return true;

        ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidParameter(name, "string")).Wait();
        return false;
    }

    public bool TryGetLongParameter(string name, out long value)
    {
        if (tryGetParameter(name, out var str) && long.TryParse(str, out value))
            return true;

        ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidParameter(name, "long")).Wait();
        value = 0;
        return false;
    }

    #endregion

    #region Query Parameters

    private bool tryGetQuery(string name, out string value)
    {
        var query = Request.QueryString.Get(name);

        if (!string.IsNullOrEmpty(query))
        {
            value = query;
            return true;
        }

        value = "";
        return false;
    }

    public bool TryGetStringQuery(string name, out string value)
    {
        if (tryGetQuery(name, out value))
            return true;

        ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.MissingQuery(name)).Wait();
        return false;
    }

    public string? GetStringQuery(string name)
        => tryGetQuery(name, out var value) ? value : null;

    public bool TryGetLongQuery(string name, out long value)
    {
        if (tryGetQuery(name, out var str) && long.TryParse(str, out value))
            return true;

        ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidQuery(name, "long")).Wait();
        value = 0;
        return false;
    }

    public int? GetIntQuery(string name)
    {
        if (tryGetQuery(name, out var str) && int.TryParse(str, out var value))
            return value;

        return null;
    }

    #endregion

    #region Headers

    private bool tryGetHeader(string name, out string value)
    {
        var header = Request.Headers.Get(name);

        if (!string.IsNullOrEmpty(header))
        {
            value = header;
            return true;
        }

        value = "";
        return false;
    }

    public bool TryGetStringHeader(string name, out string value)
    {
        if (tryGetHeader(name, out value))
            return true;

        ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.MissingHeader(name)).Wait();
        return false;
    }

    #endregion

    #region Replying

    public async Task Reply(HttpStatusCode code, object? data = null) => await reply(new APIResponse
    {
        Status = code,
        Data = data
    });

    public async Task ReplyMessage(string message) => await reply(new APIResponse
    {
        Message = message
    });

    public async Task ReplyError(HttpStatusCode code, string message) => await reply(new APIResponse
    {
        Status = code,
        Message = message
    });

    private async Task reply(APIResponse response)
    {
        response.Pagination = pagination;

        var json = response.Serialize();
        var buffer = Encoding.UTF8.GetBytes(json);
        await ReplyData(buffer, "application/json");
    }

    private bool replied;

    public async Task ReplyData(byte[] buffer, string type, string filename = "")
    {
        // worst case scenario I guess
        if (replied)
            return;

        replied = true;

        Response.ContentLength64 = buffer.Length;
        Response.ContentEncoding = Encoding.UTF8;
        Response.AddHeader("Content-Type", type);
        Response.AddHeader("Access-Control-Allow-Origin", Request.Headers.Get("Origin") ?? "*");
        Response.AddHeader("Access-Control-Allow-Methods", string.Join(", ", AllowedMethods));
        Response.AddHeader("Access-Control-Allow-Headers", string.Join(", ", AllowedHeaders));

        if (!string.IsNullOrEmpty(filename))
            Response.AddHeader("Content-Disposition", $"attachment; filename=\"{filename}\"");

        await Response.OutputStream.WriteAsync(buffer);
        Response.Close();
        stopTimer();
    }

    #endregion

    public void SetPaginationInfo(long limit, long offset, long total, long count)
        => pagination = new APIPagination(limit, offset, total, count);

    #region Timing

    private Stopwatch timer { get; } = new();
    private bool timed;

    public void StartTimer()
    {
        timer.Start();
        timed = true;
    }

    private void stopTimer()
    {
        if (!timed)
            return;

        timer.Stop();
        Logger.Log($"Request took {timer.ElapsedMilliseconds}ms", LoggingTarget.General, LogLevel.Debug);
    }

    #endregion
}
