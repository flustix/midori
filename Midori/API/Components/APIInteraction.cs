using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using HttpMultipartParser;
using Midori.Logging;
using Midori.Networking;
using HttpStatusCode = Midori.Networking.HttpStatusCode;

namespace Midori.API.Components;

public class APIInteraction
{
    protected virtual string[] AllowedMethods => new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    protected virtual string[] AllowedHeaders => new[] { "Content-Type", "Authorization", "X-Requested-With", "X-Forwarded-For" };

    protected virtual bool RespondOnInvalidParameter => true;

    protected Logger Logger { get; } = Logger.GetLogger("API");

    public HttpServerContext Context { get; private set; } = null!;
    public HttpRequest Request { get; private set; } = null!;
    public HttpResponse Response { get; } = new(HttpStatusCode.OK);

    public Dictionary<string, string> Parameters { get; private set; } = null!;
    public IPAddress RemoteIP { get; private set; } = null!;

    private MultipartFormDataParser? parser;

    public void Populate(HttpServerContext ctx, HttpRequest req, Dictionary<string, string> parameters)
    {
        Context = ctx;
        Request = req;
        Parameters = parameters;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Request.InputStream != null && Request.InputStream != Stream.Null && (Request.ContentType?.StartsWith("multipart/form-data") ?? false))
            parser = MultipartFormDataParser.Parse(Request.InputStream, Encoding.UTF8);

        var forward = Request.Headers.Get("X-Forwarded-For");
        RemoteIP = string.IsNullOrEmpty(forward) ? Context.EndPoint!.Address : IPAddress.Parse(forward.Split(",").First());

        OnPopulate();
    }

    /// <summary>
    /// Called after the interaction has been populated. Use this to set up any additional data like the current user.
    /// </summary>
    protected virtual void OnPopulate()
    {
    }

    public virtual async Task HandleRoute<T>(IAPIRoute<T> route)
        where T : APIInteraction
    {
        // there has to be a better way for this...
        if (this is T self)
            await route.Handle(self);
        else
            throw new InvalidOperationException($"The current instance is not of type {typeof(T)}.");
    }

    #region Files

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

    public FilePart? GetFile(string name)
        => TryGetFile(name, out var file) ? file : null;

    #endregion

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

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidParameter(name, "string")).Wait();

        return false;
    }

    public string? GetStringParameter(string name)
        => tryGetParameter(name, out var value) ? value : null;

    public bool TryGetIntParameter(string name, out int value)
    {
        if (tryGetParameter(name, out var str) && int.TryParse(str, out value))
            return true;

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidParameter(name, "int")).Wait();

        value = 0;
        return false;
    }

    public int? GetIntParameter(string name)
    {
        if (tryGetParameter(name, out var str) && int.TryParse(str, out var value))
            return value;

        return null;
    }

    public bool TryGetLongParameter(string name, out long value)
    {
        if (tryGetParameter(name, out var str) && long.TryParse(str, out value))
            return true;

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidParameter(name, "long")).Wait();

        value = 0;
        return false;
    }

    public long? GetLongParameter(string name)
    {
        if (tryGetParameter(name, out var str) && long.TryParse(str, out var value))
            return value;

        return null;
    }

    #endregion

    #region Query Parameters

    private bool tryGetQuery(string name, out string value)
    {
        if (Request.QueryParameters.TryGetValue(name, out var query) && !string.IsNullOrEmpty(query))
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

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.MissingQuery(name)).Wait();

        return false;
    }

    public string? GetStringQuery(string name)
        => tryGetQuery(name, out var value) ? value : null;

    public bool TryGetIntQuery(string name, out int value)
    {
        if (tryGetQuery(name, out var str) && int.TryParse(str, out value))
            return true;

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidQuery(name, "int")).Wait();

        value = 0;
        return false;
    }

    public int? GetIntQuery(string name)
    {
        if (tryGetQuery(name, out var str) && int.TryParse(str, out var value))
            return value;

        return null;
    }

    public bool TryGetLongQuery(string name, out long value)
    {
        if (tryGetQuery(name, out var str) && long.TryParse(str, out value))
            return true;

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.InvalidQuery(name, "long")).Wait();

        value = 0;
        return false;
    }

    public long? GetLongQuery(string name)
    {
        if (tryGetQuery(name, out var str) && long.TryParse(str, out var value))
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

        if (RespondOnInvalidParameter)
            ReplyError(HttpStatusCode.BadRequest, DefaultResponseStrings.MissingHeader(name)).Wait();

        return false;
    }

    public string? GetStringHeader(string name)
        => tryGetHeader(name, out var value) ? value : null;

    #endregion

    #region Replying

    private bool replied;

    public virtual async Task ReplyError(HttpStatusCode code, string error, Exception? exception = null)
    {
        var buffer = Encoding.UTF8.GetBytes(error);
        Response.StatusCode = code;
        Response.Headers.Add("Content-Type", "text/plain");
        await ReplyData(buffer);
    }

    public async Task ReplyData(byte[] buffer, string filename = "")
    {
        if (replied)
            return;

        replied = true;

        Response.ContentLength = buffer.Length;
        // Response.ContentEncoding = Encoding.UTF8;
        Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers.Get("Origin") ?? "*");
        Response.Headers.Add("Access-Control-Allow-Methods", string.Join(", ", AllowedMethods));
        Response.Headers.Add("Access-Control-Allow-Headers", string.Join(", ", AllowedHeaders));

        if (!string.IsNullOrEmpty(filename))
            Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{filename}\"");

        await Response.OutputStream.WriteAsync(buffer);
        Response.Flush();

        await Context.WriteResponse(Response);
        stopTimer();
        Context.Close();
    }

    #endregion

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
