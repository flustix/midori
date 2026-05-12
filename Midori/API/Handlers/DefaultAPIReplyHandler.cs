using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;

namespace Midori.API.Handlers;

public class DefaultAPIReplyHandler : IAPIReplyHandler
{
    public MethodInfo? TargetMethod { get; set; }

    private readonly HttpConfiguration config;

    public DefaultAPIReplyHandler(IOptions<HttpConfiguration> config)
    {
        this.config = config.Value;
    }

    public void Handle<T>(HttpServerContext ctx, APIReturn<T> ret)
    {
        var rsp = new HttpResponse(ret.Status);
        config.ApplyHeaders(rsp.Headers);

        var ct = TargetMethod?.GetCustomAttribute<ReturnsMimeAttribute>()?.MimeType ?? "application/json";
        rsp.ContentType = ct;

        if (ret.Result is Stream stream)
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            var startPos = stream.Position;
            stream.CopyTo(rsp.BodyStream);
            rsp.ContentLength = stream.Position - startPos;
            stream.Dispose();
        }
        else
        {
            var bytes = Encoding.UTF8.GetBytes(ret.Serialize());
            rsp.BodyStream.Write(bytes);
            rsp.ContentLength = bytes.Length;
        }

        try
        {
            rsp.WriteToStream(ctx.Stream).Wait();
        }
        catch (Exception ex)
        {
            // "Unable to write data to the transport connection: Broken pipe."
            if (ex is AggregateException { InnerException: IOException { HResult: -2146232800 } })
                return;

            throw;
        }
        finally
        {
            ctx.Dispose();
            rsp.Dispose();
        }
    }
}
