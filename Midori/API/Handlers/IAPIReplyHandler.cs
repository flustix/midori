using System.Reflection;
using Midori.API.Components;
using Midori.Networking;
using Midori.Networking.Handlers;

namespace Midori.API.Handlers;

public interface IAPIReplyHandler : IHttpReplyHandler
{
    MethodInfo? TargetMethod { get; set; }

    void Handle<T>(HttpServerContext ctx, APIReturn<T> ret);

    void IHttpReplyHandler.Handle(HttpServerContext ctx, HttpStatusCode code, Exception? error)
    {
        var ret = (APIReturn<object>)new StatusReturn(code, "");
        ret.Exception = error;
        Handle(ctx, ret);
    }
}
