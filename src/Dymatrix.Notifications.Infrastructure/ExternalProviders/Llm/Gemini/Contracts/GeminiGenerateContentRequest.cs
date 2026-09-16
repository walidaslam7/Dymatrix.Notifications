namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed record GeminiGenerateContentRequest(
    IReadOnlyCollection<GeminiRequestContent> Contents,
    GeminiGenerationConfig GenerationConfig);
