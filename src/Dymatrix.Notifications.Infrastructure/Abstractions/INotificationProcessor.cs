using Dymatrix.Notifications.Domain.Models;

namespace Dymatrix.Notifications.Infrastructure.Abstractions;

public interface INotificationProcessor
{
    Task ProcessAsync(
        Notification notification,
        CancellationToken cancellationToken);
}
