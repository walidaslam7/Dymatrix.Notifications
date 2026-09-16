using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiGenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; init; }
}
