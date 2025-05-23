﻿using Midori.Utils;

namespace Midori.Networking.WebSockets.Typed.Proxy;

public class TypedServerProxy : ITypedProxy, ITypedSingleProxy
{
    private Dictionary<string, TypedResponseWaitInfo> waitInfos { get; }
    private WebSocket socket { get; }

    internal TypedServerProxy(WebSocket socket, Dictionary<string, TypedResponseWaitInfo> waitInfos)
    {
        this.socket = socket;
        this.waitInfos = waitInfos;
    }

    public async Task PerformAsync(string method, object?[] args, CancellationToken token)
    {
        var request = TypedInvokeRequest.Create(method, args);
        await socket.SendTextAsync(request.Serialize());
    }

    public async Task<T> PerformWithReturnAsync<T>(string method, object?[] args, CancellationToken token)
    {
        var request = TypedInvokeRequest.Create(method, args);

        var tsc = new TaskCompletionSource<T>();
        waitInfos.Add(request.InvokeID, new TypedResponseWaitInfo(res => tsc.SetResult((T)res!), tsc.SetException, typeof(T)));

        await socket.SendTextAsync(request.Serialize());
        await Task.WhenAny(tsc.Task, Task.Delay(5000, token));

        if (!tsc.Task.IsCompleted)
            throw new TimeoutException("The request timed out!");

        return await tsc.Task;
    }
}
