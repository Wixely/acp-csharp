using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

/// <summary>
/// Optional session capabilities the agent advertises during initialization.
/// Supplying an (empty) capability object means the corresponding method is supported;
/// omitting it (or null) means it is not. `session/load` remains governed by the
/// top-level `loadSession` capability.
/// </summary>
public record SessionCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("list")]
    public SessionListCapabilities? List { get; init; }

    [JsonPropertyName("delete")]
    public SessionDeleteCapabilities? Delete { get; init; }

    [JsonPropertyName("resume")]
    public SessionResumeCapabilities? Resume { get; init; }

    [JsonPropertyName("close")]
    public SessionCloseCapabilities? Close { get; init; }

    [JsonPropertyName("additionalDirectories")]
    public SessionAdditionalDirectoriesCapabilities? AdditionalDirectories { get; init; }
}

public record SessionListCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public record SessionDeleteCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public record SessionResumeCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public record SessionCloseCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public record SessionAdditionalDirectoriesCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}
