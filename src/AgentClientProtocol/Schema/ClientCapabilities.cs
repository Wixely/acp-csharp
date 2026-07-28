using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

public record ClientCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("fs")]
    public FileSystemCapability Fs { get; init; } = new();

    [JsonPropertyName("terminal")]
    public bool Terminal { get; init; } = false;

    /// <summary>Session-related client extensions; null means none advertised.</summary>
    [JsonPropertyName("session")]
    public ClientSessionCapabilities? Session { get; init; }
}

public record ClientSessionCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    /// <summary>Config-option extensions the client supports; null means none.</summary>
    [JsonPropertyName("configOptions")]
    public SessionConfigOptionsCapabilities? ConfigOptions { get; init; }
}

/// <summary>
/// Config-option capabilities advertised by the client. A non-null <see cref="Boolean"/>
/// (even empty <c>{}</c>) means the agent may include <c>type: "boolean"</c> options;
/// select options are always permitted regardless.
/// </summary>
public record SessionConfigOptionsCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("boolean")]
    public BooleanConfigOptionCapabilities? Boolean { get; init; }
}

public record BooleanConfigOptionCapabilities
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public record FileSystemCapability
{
    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }

    [JsonPropertyName("readTextFile")]
    public bool ReadTextFile { get; init; } = false;

    [JsonPropertyName("writeTextFile")]
    public bool WriteTextFile { get; init; } = false;
}
