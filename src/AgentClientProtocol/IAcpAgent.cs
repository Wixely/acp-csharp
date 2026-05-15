using System.Text.Json;

namespace AgentClientProtocol;

public interface IAcpAgent
{
    ValueTask<InitializeResponse> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken = default);
    ValueTask<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default);
    ValueTask<NewSessionResponse> NewSessionAsync(NewSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken = default);
    ValueTask CancelAsync(CancelNotification notification, CancellationToken cancellationToken = default);
    ValueTask<LoadSessionResponse> LoadSessionAsync(LoadSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<SetSessionModeResponse> SetSessionModeAsync(SetSessionModeRequest request, CancellationToken cancellationToken = default);
    ValueTask<SetSessionModelResponse> SetSessionModelAsync(SetSessionModelRequest request, CancellationToken cancellationToken = default);
    ValueTask<ListSessionsResponse> ListSessionsAsync(ListSessionsRequest request, CancellationToken cancellationToken = default);
    ValueTask<ForkSessionResponse> ForkSessionAsync(ForkSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<ResumeSessionResponse> ResumeSessionAsync(ResumeSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<CloseSessionResponse> CloseSessionAsync(CloseSessionRequest request, CancellationToken cancellationToken = default);
    ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default);
    ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default);
}