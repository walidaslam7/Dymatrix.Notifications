using Dymatrix.Notifications.Domain.Models;

namespace Dymatrix.Notifications.Application.Abstractions;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(
        Notification notification,
        CancellationToken cancellationToken);
}