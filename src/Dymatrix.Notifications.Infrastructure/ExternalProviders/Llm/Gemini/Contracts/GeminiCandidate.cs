using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiResponseContent? Content { get; init; }
}
