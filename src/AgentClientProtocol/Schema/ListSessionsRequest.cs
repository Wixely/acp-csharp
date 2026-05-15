using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record ListSessionsRequest
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public string[]? AdditionalDirectories { get; init; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }
}
