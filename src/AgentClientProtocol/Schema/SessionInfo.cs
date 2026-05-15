using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record SessionInfo
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public string[]? AdditionalDirectories { get; init; }

    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; init; }
}
