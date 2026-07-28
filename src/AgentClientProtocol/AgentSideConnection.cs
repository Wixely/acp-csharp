using System.Text.Json;

namespace AgentClientProtocol;

public sealed class AgentSideConnection : IDisposable, IAcpClient
{
    readonly CancellationTokenSource cts = new();
    readonly JsonRpcEndpoint endpoint;

    public AgentSideConnection(IAcpAgent agent, TextReader reader, TextWriter writer)
        : this(_ => agent, reader, writer)
    {
    }

    public AgentSideConnection(Func<IAcpClient, IAcpAgent> toAgent, TextReader reader, TextWriter writer)
    {
        var agent = toAgent(this);

        endpoint = new(
            _ => new(reader.ReadLine()),
            (s, _) =>
            {
                writer.WriteLine(s);
                return default;
            },
            (s, _) => default
        );

        endpoint.SetRequestHandler(AgentMethods.Initialize, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.InitializeAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<InitializeRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<InitializeResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.Authenticate, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.AuthenticateAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<AuthenticateRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<AuthenticateResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.SessionNew, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.NewSessionAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<NewSessionRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<NewSessionResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.SessionPrompt, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.PromptAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<PromptRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<PromptResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.SessionLoad, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.LoadSessionAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<LoadSessionRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<LoadSessionResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.SessionSetMode, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.SetSessionModeAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<SetSessionModeRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<SetSessionModeResponse>())
            };
        });

        endpoint.SetRequestHandler(AgentMethods.SessionSetModel, async (request, ct) =>
        {
            AcpException.ThrowIfParamIsNull(request.Params);

            var response = await agent.SetSessionModelAsync(JsonSerializer.Deserialize(
                request.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<SetSessionModelRequest>())!, ct);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(response, AcpJsonSerializerContext.Default.Options.GetTypeInfo<SetSessionModelResponse>())
            };
        });

        endpoint.SetNotificationHandler(AgentMethods.SessionCancel, async (notification, ct) =>
        {
            AcpException.ThrowIfParamIsNull(notification.Params);

            var cancelNotification = JsonSerializer.Deserialize(
                notification.Params!.Value,
                AcpJsonSerializerContext.Default.Options.GetTypeInfo<CancelNotification>())!;

            await agent.CancelAsync(cancelNotification, ct);
        });

        endpoint.SetDefaultRequestHandler(async (request, ct) =>
        {
            var response = await agent.ExtMethodAsync(request.Method, request.Params ?? default, ct);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = response
            };
        });

        endpoint.SetDefaultNotificationHandler(async (notification, ct) =>
        {
            await agent.ExtNotificationAsync(notification.Method, notification.Params ?? default, ct);
        });
    }

    async ValueTask<TResponse> RequestAsync<TRequest, TResponse>(string method, TRequest request, CancellationToken cancellationToken)
    {
        var response = await endpoint.SendRequestAsync(new JsonRpcRequest
        {
            Method = method,
            Id = default,
            Params = JsonSerializer.SerializeToElement(request, AcpJsonSerializerContext.Default.Options.GetTypeInfo<TRequest>())
        }, cancellationToken);

        if (response.Error != null)
        {
            throw new AcpException($"{response.Error!.Message}", response.Error.Data, response.Error.Code);
        }

        if (response.Result == null)
        {
            return default!;
        }

        return JsonSerializer.Deserialize(response.Result.Value, AcpJsonSerializerContext.Default.Options.GetTypeInfo<TResponse>())!;
    }

    async ValueTask NotificationAsync<TNotification>(string method, TNotification notification, CancellationToken cancellationToken)
    {
        await endpoint.SendMessageAsync(new JsonRpcNotification
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(notification, AcpJsonSerializerContext.Default.Options.GetTypeInfo<TNotification>())
        }, cancellationToken);
    }

    public ValueTask SessionNotificationAsync(SessionNotification notification, CancellationToken cancellationToken = default)
    {
        return NotificationAsync(ClientMethods.SessionUpdate, notification, cancellationToken);
    }

    public ValueTask<RequestPermissionResponse> RequestPermissionAsync(RequestPermissionRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<RequestPermissionRequest, RequestPermissionResponse>(ClientMethods.SessionRequestPermission, request, cancellationToken);
    }

    public ValueTask<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<ReadTextFileRequest, ReadTextFileResponse>(ClientMethods.FsReadTextFile, request, cancellationToken);
    }

    public ValueTask<WriteTextFileResponse> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<WriteTextFileRequest, WriteTextFileResponse>(ClientMethods.FsWriteTextFile, request, cancellationToken);
    }

    public ValueTask<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<CreateTerminalRequest, CreateTerminalResponse>(ClientMethods.TerminalCreate, request, cancellationToken);
    }

    public ValueTask<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<TerminalOutputRequest, TerminalOutputResponse>(ClientMethods.TerminalOutput, request, cancellationToken);
    }

    public ValueTask<ReleaseTerminalResponse> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<ReleaseTerminalRequest, ReleaseTerminalResponse>(ClientMethods.TerminalRelease, request, cancellationToken);
    }

    public ValueTask<WaitForTerminalExitResponse> WaitForTerminalExitAsync(WaitForTerminalExitRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<WaitForTerminalExitRequest, WaitForTerminalExitResponse>(ClientMethods.TerminalWaitForExit, request, cancellationToken);
    }

    public ValueTask<KillTerminalCommandResponse> KillTerminalCommandAsync(KillTerminalCommandRequest request, CancellationToken cancellationToken = default)
    {
        return RequestAsync<KillTerminalCommandRequest, KillTerminalCommandResponse>(ClientMethods.TerminalKill, request, cancellationToken);
    }

    public async ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default)
    {
        var response = await endpoint.SendRequestAsync(new JsonRpcRequest
        {
            Method = method,
            Id = default,
            Params = request,
        }, cancellationToken);

        if (response.Result == null)
        {
            throw new AcpException($"{response.Error!.Message}", response.Error.Data, response.Error.Code);
        }

        return response.Result.Value;
    }

    public ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default)
    {
        return endpoint.SendMessageAsync(new JsonRpcNotification
        {
            Method = method,
            Params = notification
        }, cancellationToken);
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    public void Open()
    {
        Task.Run(async () => await endpoint.ReadMessagesAsync(cts.Token));
    }
}