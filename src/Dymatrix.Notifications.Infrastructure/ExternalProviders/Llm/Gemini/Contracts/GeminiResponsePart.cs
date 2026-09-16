using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiResponsePart
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
