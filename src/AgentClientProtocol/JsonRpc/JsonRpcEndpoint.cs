using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentClientProtocol;

internal enum JsonRpcErrorCode
{
    ParseError = -32700,
    InvalidRequest = -32600,
    MethodNotFound = -32601,
    InvalidParams = -32602,
    InternalError = -32603
}

internal sealed class JsonRpcEndpoint(Func<CancellationToken, ValueTask<string?>> readFunc, Func<string, CancellationToken, ValueTask> writeFunc, Func<string, CancellationToken, ValueTask> errorWriteFunc)
{
    readonly ConcurrentDictionary<RequestId, TaskCompletionSource<JsonRpcResponse>> pendingRequests = new();
    readonly SemaphoreSlim writeLock = new(1, 1);
    readonly ConcurrentDictionary<string, Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>>> requestHandlers = new();
    readonly ConcurrentDictionary<string, Func<JsonRpcNotification, CancellationToken, ValueTask>> notificationHandlers = new();
    Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>>? defaultRequestHandler;
    Func<JsonRpcNotification, CancellationToken, ValueTask>? defaultNotificationHandler;
    int nextRequestId = 0;

    public void SetRequestHandler(string method, Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>> handler)
    {
        requestHandlers.TryAdd(method, handler);
    }

    public void SetNotificationHandler(string method, Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
    {
        notificationHandlers.TryAdd(method, handler);
    }

    public void SetDefaultRequestHandler(Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcResponse>> handler)
    {
        defaultRequestHandler = handler;
    }

    public void SetDefaultNotificationHandler(Func<JsonRpcNotification, CancellationToken, ValueTask> handler)
    {
        defaultNotificationHandler = handler;
    }

    public async Task ReadMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ReadMessagesCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // No more responses can ever arrive — fail outstanding requests instead of
            // leaving their callers awaiting forever (e.g. an agent blocked on
            // session/request_permission after the client process died).
            foreach (var id in pendingRequests.Keys)
            {
                if (pendingRequests.TryRemove(id, out var tcs))
                {
                    tcs.TrySetException(new AcpException("Connection closed before a response was received", null, (int)JsonRpcErrorCode.InternalError));
                }
            }
        }
    }

    async Task ReadMessagesCoreAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await readFunc(cancellationToken).ConfigureAwait(false);
                if (line is null) return; // EOF — the peer closed the stream
                if (string.IsNullOrWhiteSpace(line)) continue;

                var trimmedLineSpan = line.AsSpan().Trim();
                if (trimmedLineSpan.Length < 2 || trimmedLineSpan[0] != '{' || trimmedLineSpan[^1] != '}') continue; // skip non-json input

                var message = JsonSerializer.Deserialize(line, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()!);

                switch (message)
                {
                    case JsonRpcRequest request:
                        // Dispatch without awaiting: a long-running handler (e.g. an agent's
                        // session/prompt) must not block the read loop, or an in-flight
                        // session/cancel could never be processed and a handler that calls
                        // back to the peer (e.g. session/request_permission) would deadlock
                        // waiting for a response this loop can no longer read.
                        _ = Task.Run(() => HandleRequestAsync(request, cancellationToken).AsTask(), cancellationToken);
                        break;
                    case JsonRpcResponse response:
                        {
                            if (pendingRequests.TryRemove(response.Id, out var tcs))
                            {
                                tcs.TrySetResult(response);
                            }
                        }
                        break;
                    case JsonRpcNotification notification:
                        // Notifications are handled inline: session/update chunks must be
                        // delivered in the order they arrived, which Task.Run would not
                        // guarantee. This still cannot starve anything — the long-running
                        // work (requests) runs off-loop, so a session/cancel notification
                        // is processed while the prompt request it targets is in flight.
                        await HandleNotificationAsync(notification, cancellationToken);
                        break;
                    default:
                        throw new AcpException($"Invalid response type: {message?.GetType().Name}", null, (int)JsonRpcErrorCode.InternalError);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                await errorWriteFunc(ex.ToString(), cancellationToken);
            }
        }
    }

    async ValueTask HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (requestHandlers.TryGetValue(request.Method, out var requestHandler))
            {
                var response = await requestHandler(request, cancellationToken);
                await WriteAsync(JsonSerializer.Serialize(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
            }
            else if (defaultRequestHandler != null)
            {
                var response = await defaultRequestHandler(request, cancellationToken);
                await WriteAsync(JsonSerializer.Serialize(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
            }
            else
            {
                await WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new()
                    {
                        Code = (int)JsonRpcErrorCode.MethodNotFound,
                        Message = $"Method '{request.Method}' is not available",
                    }
                }, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
            }
        }
        catch (NotImplementedException)
        {
            await WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
            {
                Id = request.Id,
                Error = new()
                {
                    Code = (int)JsonRpcErrorCode.MethodNotFound,
                    Message = $"Method '{request.Method}' is not available",
                }
            }, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
        }
        catch (AcpException acpException)
        {
            await WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
            {
                Id = request.Id,
                Error = new()
                {
                    Code = acpException.Code,
                    Data = acpException.ErrorData,
                    Message = acpException.Message,
                }
            }, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
            {
                Id = request.Id,
                Error = new()
                {
                    Code = (int)JsonRpcErrorCode.InternalError,
                    Message = ex.Message,
                }
            }, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>()), cancellationToken);
        }
    }

    async ValueTask HandleNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notificationHandlers.TryGetValue(notification.Method, out var notificationHandler))
            {
                await notificationHandler(notification, cancellationToken);
            }
            else if (defaultNotificationHandler != null)
            {
                await defaultNotificationHandler(notification, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await errorWriteFunc(ex.ToString(), cancellationToken);
        }
    }

    async ValueTask WriteAsync(string json, CancellationToken cancellationToken)
    {
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await writeFunc(json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async ValueTask SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (message is JsonRpcRequest request && !request.Id.IsValid)
        {
            request.Id = Interlocked.Increment(ref nextRequestId);
        }

        var json = JsonSerializer.Serialize(message, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcMessage>());
        await WriteAsync(json, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Id.IsValid) request.Id = Interlocked.Increment(ref nextRequestId);

        var json = JsonSerializer.Serialize(request, AcpJsonSerializerContext.Default.Options.GetTypeInfo<JsonRpcRequest>());

        var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingRequests.TryAdd(request.Id, tcs);

        try
        {
            using var cancelRegistration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            await WriteAsync(json, cancellationToken).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            pendingRequests.TryRemove(request.Id, out _);
        }
    }
}