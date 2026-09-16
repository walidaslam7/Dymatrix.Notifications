using System.Text.Json;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Contracts;
using Dymatrix.Notifications.Infrastructure.Models;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Serialization;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Mapping;

public sealed class GeminiResponseMapper
{
    public GeneratedMessage Map(GeminiGenerateContentResponse? providerResponse)
    {
        var generatedJson = providerResponse?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(generatedJson))
        {
            throw new InvalidDataException("The Gemini provider returned no usable generated content.");
        }

        GeminiGeneratedMessage? generated;
        try
        {
            generated = JsonSerializer.Deserialize<GeminiGeneratedMessage>(
                generatedJson,
                GeminiJsonSerializerOptions.GeneratedOutputJsonOptions);
        }
        catch (JsonException exception)
        {
            throw new JsonException("The Gemini provider returned malformed structured output.", exception);
        }

        if (generated is null || string.IsNullOrWhiteSpace(generated.Category))
        {
            throw new InvalidDataException("The generated category was empty.");
        }

        if (string.IsNullOrWhiteSpace(generated.Message))
        {
            throw new InvalidDataException("The generated message was empty.");
        }

        return new GeneratedMessage(generated.Category, generated.Message);
    }
}
