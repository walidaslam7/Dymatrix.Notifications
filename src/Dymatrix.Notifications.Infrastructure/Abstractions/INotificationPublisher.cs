using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Models;

namespace Dymatrix.Notifications.Infrastructure.Abstractions;

public interface INotificationPublisher
{
    Task PublishAsync(
        Notification notification,
        GeneratedMessage generatedMessage,
        CancellationToken cancellationToken);
}
