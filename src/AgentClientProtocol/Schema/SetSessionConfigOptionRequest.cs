using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

/// <summary>
/// Sets a session configuration option to a new value (<c>session/set_config_option</c>).
/// The value is a flattened union: for a boolean option, <see cref="Type"/> is <c>"boolean"</c>
/// and <see cref="Value"/> holds a JSON boolean; otherwise the option is a select and
/// <see cref="Value"/> holds the chosen option's value id (a string). Use <see cref="AsBoolean"/>
/// / <see cref="AsValueId"/> to read it.
/// </summary>
public record SetSessionConfigOptionRequest
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("configId")]
    public required string ConfigId { get; init; }

    /// <summary><c>"boolean"</c> for a boolean option; absent (null) for a select value id.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }

    /// <summary>The boolean payload, or null when this isn't a boolean value.</summary>
    public bool? AsBoolean() =>
        Value.ValueKind is JsonValueKind.True or JsonValueKind.False ? Value.GetBoolean() : null;

    /// <summary>The select option's value id, or null when the payload isn't a string.</summary>
    public string? AsValueId() =>
        Value.ValueKind == JsonValueKind.String ? Value.GetString() : null;
}
