using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;

namespace Dymatrix.Notifications.Infrastructure.Processing;

public sealed class NotificationProcessor(
    IMessageGenerator messageGenerator,
    IOutboundNotificationRateLimiter rateLimiter,
    INotificationPublisher publisher) : INotificationProcessor
{
    public async Task ProcessAsync(Notification notification, CancellationToken cancellationToken)
    {
        var generatedMessage = await messageGenerator.GenerateAsync(notification, cancellationToken);
        await rateLimiter.AcquireAsync(cancellationToken);
        await publisher.PublishAsync(notification, generatedMessage, cancellationToken);
    }
}