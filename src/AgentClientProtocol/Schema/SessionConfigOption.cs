using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

/// <summary>
/// A session configuration option the agent exposes for the client to display and change
/// (via <c>session/set_config_option</c>). Discriminated by <c>type</c> into a single-value
/// <see cref="SelectSessionConfigOption"/> (dropdown) or a <see cref="BooleanSessionConfigOption"/>
/// (toggle). Boolean options are only valid when the client advertises
/// <see cref="SessionConfigOptionsCapabilities.Boolean"/>; select options are always allowed.
/// </summary>
[JsonConverter(typeof(SessionConfigOptionJsonConverter))]
public abstract record SessionConfigOption
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Optional UX-only category. Well-known values: <c>mode</c>, <c>model</c>,
    /// <c>model_config</c>, <c>thought_level</c>; any other string is treated as uncategorised.
    /// Modelled as a string so unknown categories round-trip without error.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public static class SessionConfigOptionCategories
{
    public const string Mode = "mode";
    public const string Model = "model";
    public const string ModelConfig = "model_config";
    public const string ThoughtLevel = "thought_level";
}

public record SelectSessionConfigOption : SessionConfigOption
{
    [JsonPropertyName("type")]
    public override string Type => "select";

    [JsonPropertyName("currentValue")]
    public required string CurrentValue { get; init; }

    /// <summary>
    /// The selectable options. Only the spec's ungrouped (flat) form is modelled here, which is
    /// what agents normally emit; grouped options are not represented.
    /// </summary>
    [JsonPropertyName("options")]
    public required SessionConfigSelectOption[] Options { get; init; }
}

public record BooleanSessionConfigOption : SessionConfigOption
{
    [JsonPropertyName("type")]
    public override string Type => "boolean";

    [JsonPropertyName("currentValue")]
    public required bool CurrentValue { get; init; }
}

public record SessionConfigSelectOption
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("_meta")]
    public JsonElement? Meta { get; init; }
}

public class SessionConfigOptionJsonConverter : JsonConverter<SessionConfigOption>
{
    public override SessionConfigOption? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Missing 'type' property in SessionConfigOption");
        }

        return typeProperty.GetString() switch
        {
            "select" => root.Deserialize(AcpJsonSerializerContext.Default.Options.GetTypeInfo<SelectSessionConfigOption>()),
            "boolean" => root.Deserialize(AcpJsonSerializerContext.Default.Options.GetTypeInfo<BooleanSessionConfigOption>()),
            var t => throw new JsonException($"Unknown SessionConfigOption type: {t}")
        };
    }

    public override void Write(Utf8JsonWriter writer, SessionConfigOption value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case SelectSessionConfigOption s:
                JsonSerializer.Serialize(writer, s, AcpJsonSerializerContext.Default.Options.GetTypeInfo<SelectSessionConfigOption>());
                break;
            case BooleanSessionConfigOption b:
                JsonSerializer.Serialize(writer, b, AcpJsonSerializerContext.Default.Options.GetTypeInfo<BooleanSessionConfigOption>());
                break;
            default:
                throw new JsonException($"Unknown SessionConfigOption subtype: {value.GetType()}");
        }
    }
}
