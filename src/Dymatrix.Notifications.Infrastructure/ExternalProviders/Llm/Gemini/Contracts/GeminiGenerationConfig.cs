namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed record GeminiGenerationConfig(
    string ResponseMimeType,
    GeminiResponseSchema ResponseSchema);
