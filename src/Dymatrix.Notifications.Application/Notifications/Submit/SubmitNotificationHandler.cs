using Dymatrix.Notifications.Application.Abstractions;
using Dymatrix.Notifications.Domain.Models;

namespace Dymatrix.Notifications.Application.Notifications.Submit;

public sealed class SubmitNotificationHandler(INotificationQueue notificationQueue)
{
    public async Task<SubmitNotificationOutcome> HandleAsync(
        SubmitNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification(command.Level, command.Message);

        if (!notification.RequiresForwarding)
        {
            return new SubmitNotificationOutcome(notification.Id, Queued: false);
        }

        await notificationQueue.EnqueueAsync(notification, cancellationToken);

        return new SubmitNotificationOutcome(notification.Id, Queued: true);
    }
}