using Dymatrix.Notifications.Domain.Models;

namespace Dymatrix.Notifications.Infrastructure.Abstractions;

public interface INotificationQueueReader
{
    IAsyncEnumerable<Notification> ReadAllAsync(
        CancellationToken cancellationToken);
}
