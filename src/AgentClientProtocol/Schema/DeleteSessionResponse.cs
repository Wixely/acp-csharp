using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record DeleteSessionResponse
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}
