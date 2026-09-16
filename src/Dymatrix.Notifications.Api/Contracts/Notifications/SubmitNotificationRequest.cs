using Dymatrix.Notifications.Domain.Constants;

namespace Dymatrix.Notifications.Api.Contracts.Notifications;

public sealed record SubmitNotificationRequest(
    NotificationLevel Level,
    string Message);
