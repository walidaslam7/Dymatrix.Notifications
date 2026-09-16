using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiErrorResponse
{
    [JsonPropertyName("error")]
    public GeminiError? Error { get; init; }
}
