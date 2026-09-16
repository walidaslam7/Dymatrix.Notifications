using Dymatrix.Notifications.Domain.Constants;

namespace Dymatrix.Notifications.Application.Notifications.Submit;

public sealed record SubmitNotificationCommand(
    NotificationLevel Level,
    string Message);