namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;

public sealed record GeminiResponseSchema(
    string Type,
    GeminiResponseSchemaProperties Properties,
    IReadOnlyCollection<string> Required);

public sealed record GeminiResponseSchemaProperties(
    GeminiSchemaProperty Category,
    GeminiSchemaProperty Message);

public sealed record GeminiSchemaProperty(string Type);
