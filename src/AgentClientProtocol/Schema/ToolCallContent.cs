using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentClientProtocol;

[JsonConverter(typeof(ToolCallContentJsonConverter))]
public abstract record ToolCallContent
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public record ContentToolCallContent : ToolCallContent
{
    [JsonPropertyName("type")]
    public override string Type => "content";

    [JsonPropertyName("content")]
    public required ContentBlock Content { get; init; }
}

public record DiffToolCallContent : ToolCallContent
{
    [JsonPropertyName("type")]
    public override string Type => "diff";

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("newText")]
    public required string NewText { get; init; }

    [JsonPropertyName("oldText")]
    public string? OldText { get; init; }
}

public record TerminalToolCallContent : ToolCallContent
{
    [JsonPropertyName("type")]
    public override string Type => "terminal";

    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }
}

public class ToolCallContentJsonConverter : JsonConverter<ToolCallContent>
{
    public override ToolCallContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("ToolCallContent must be a JSON object");
        }

        if (root.TryGetProperty("type", out var typeProperty))
        {
            return typeProperty.GetString() switch
            {
                "content" => root.Deserialize<ContentToolCallContent>(options),
                "diff" => root.Deserialize<DiffToolCallContent>(options),
                "terminal" => root.Deserialize<TerminalToolCallContent>(options),
                var t => throw new JsonException($"Unknown ToolCallContent type: {t}")
            };
        }

        // Lenient fallback for peers that omit the discriminator: infer from the payload shape.
        if (root.TryGetProperty("content", out _))
        {
            return root.Deserialize<ContentToolCallContent>(options);
        }
        else if (root.TryGetProperty("terminalId", out _))
        {
            return root.Deserialize<TerminalToolCallContent>(options);
        }
        else if (root.TryGetProperty("path", out _) || root.TryGetProperty("newText", out _))
        {
            return root.Deserialize<DiffToolCallContent>(options);
        }

        throw new JsonException("Unknown ToolCallContent type - missing discriminator properties");
    }

    public override void Write(Utf8JsonWriter writer, ToolCallContent value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
