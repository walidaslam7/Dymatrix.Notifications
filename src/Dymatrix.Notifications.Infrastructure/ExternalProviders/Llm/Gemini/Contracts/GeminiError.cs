using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed class GeminiError
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
