using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record ResumeSessionRequest
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public string[]? AdditionalDirectories { get; init; }

    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    [JsonPropertyName("mcpServers")]
    public McpServer[]? McpServers { get; init; }

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
}
