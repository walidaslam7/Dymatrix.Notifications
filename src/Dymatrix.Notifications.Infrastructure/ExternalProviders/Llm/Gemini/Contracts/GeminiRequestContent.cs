namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed record GeminiRequestContent(IReadOnlyCollection<GeminiRequestPart> Parts);
