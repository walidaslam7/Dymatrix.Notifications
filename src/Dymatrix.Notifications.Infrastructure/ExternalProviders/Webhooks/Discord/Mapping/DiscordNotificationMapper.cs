using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Contracts;
using Dymatrix.Notifications.Infrastructure.Models;

namespace Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Mapping;

public sealed class DiscordNotificationMapper
{
    public DiscordWebhookMessage Map(Notification notification, GeneratedMessage generatedMessage)
    {
        var content = $"[{notification.Level.ToString().ToUpperInvariant()}] {generatedMessage.Category}\n{generatedMessage.Message}";

        return new DiscordWebhookMessage(content);
    }
}
