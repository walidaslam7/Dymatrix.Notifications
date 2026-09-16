using System.Net.Http.Json;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Contracts;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Client;

public sealed class DiscordWebhookClient(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options)
{
    internal const string ClientName = nameof(DiscordWebhookClient);

    private readonly DiscordOptions _options = options.Value;

    public async Task PublishAsync(DiscordWebhookMessage message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.WebhookUrl!));
        request.Content = JsonContent.Create(message);

        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory
                .CreateClient(ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("The Discord webhook request failed.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"The Discord webhook returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                    inner: null,
                    response.StatusCode);
            }
        }
    }
}
