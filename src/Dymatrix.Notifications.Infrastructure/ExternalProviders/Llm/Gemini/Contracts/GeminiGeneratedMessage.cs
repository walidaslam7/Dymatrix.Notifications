using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiGeneratedMessage
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
