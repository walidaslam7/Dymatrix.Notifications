using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Mapping;
using Dymatrix.Notifications.Infrastructure.Models;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord;

public sealed class DiscordNotificationPublisher(
    DiscordNotificationMapper mapper,
    DiscordWebhookClient client) : INotificationPublisher
{
    public Task PublishAsync(
        Notification notification,
        GeneratedMessage generatedMessage,
        CancellationToken cancellationToken) =>
        client.PublishAsync(mapper.Map(notification, generatedMessage), cancellationToken);
}
