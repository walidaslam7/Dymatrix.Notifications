namespace Dymatrix.Notifications.Infrastructure.Abstractions;

public interface IOutboundNotificationRateLimiter
{
    ValueTask AcquireAsync(CancellationToken cancellationToken);
}
