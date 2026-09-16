namespace Dymatrix.Notifications.Api.Contracts.Notifications;

public sealed record SubmitNotificationResponse(
    Guid NotificationId,
    bool Queued);
