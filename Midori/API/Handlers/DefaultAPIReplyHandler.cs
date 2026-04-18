using System.Text;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;

namespace Midori.API.Handlers;

public class DefaultAPIReplyHandler : IAPIReplyHandler
{
    public void Handle<T>(HttpServerContext ctx, APIReturn<T> ret)
    {
        var rsp = new HttpResponse(ret.Status);

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
