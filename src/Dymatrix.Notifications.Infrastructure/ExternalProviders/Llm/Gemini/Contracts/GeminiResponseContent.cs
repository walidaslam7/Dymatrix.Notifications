using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiResponseContent
{
    [JsonPropertyName("parts")]
    public List<GeminiResponsePart>? Parts { get; init; }
}
