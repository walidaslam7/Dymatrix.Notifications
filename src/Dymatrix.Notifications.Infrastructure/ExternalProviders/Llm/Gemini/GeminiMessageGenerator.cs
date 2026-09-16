using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Mapping;
using Dymatrix.Notifications.Infrastructure.Models;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini;

public sealed class GeminiMessageGenerator(
    GeminiRequestMapper requestMapper,
    GeminiClient client,
    GeminiResponseMapper responseMapper) : IMessageGenerator
{
    public async Task<GeneratedMessage> GenerateAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        var request = requestMapper.Map(notification);
        var response = await client.GenerateContentAsync(request, cancellationToken);

        return responseMapper.Map(response);
    }
}