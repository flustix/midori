using System.Text;
using Microsoft.Extensions.Options;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;

namespace Midori.API.Handlers;

public class DefaultAPIReplyHandler : IAPIReplyHandler
{
    private readonly HttpConfiguration config;

    public DefaultAPIReplyHandler(IOptions<HttpConfiguration> config)
    {
        this.config = config.Value;
    }

    public void Handle<T>(HttpServerContext ctx, APIReturn<T> ret)
    {
        var rsp = new HttpResponse(ret.Status);
        config.ApplyHeaders(rsp.Headers);
        rsp.Headers["Content-Type"] = "application/json";

        var bytes = Encoding.UTF8.GetBytes(ret.Serialize());
        rsp.BodyStream.Write(bytes);

        rsp.ContentLength = bytes.Length;

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
