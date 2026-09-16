namespace Dymatrix.Notifications.Application.Notifications.Submit;

public sealed record SubmitNotificationOutcome(
    Guid NotificationId,
    bool Queued);