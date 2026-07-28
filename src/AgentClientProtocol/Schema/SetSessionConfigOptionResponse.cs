using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record SetSessionConfigOptionResponse
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    /// <summary>The full, updated set of config options after applying the change.</summary>
    [JsonPropertyName("configOptions")]
    public required SessionConfigOption[] ConfigOptions { get; init; }
}
