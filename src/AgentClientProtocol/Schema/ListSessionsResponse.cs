using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record ListSessionsResponse
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; init; }

    [JsonPropertyName("sessions")]
    public required SessionInfo[] Sessions { get; init; }
}
