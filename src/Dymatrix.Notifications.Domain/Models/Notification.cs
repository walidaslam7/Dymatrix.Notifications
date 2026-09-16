using Dymatrix.Notifications.Domain.Constants;

namespace Dymatrix.Notifications.Domain.Models;

public sealed class Notification(NotificationLevel level, string message)
{
    public Guid Id { get; } = Guid.NewGuid();

    public NotificationLevel Level { get; } = level;

    public string Message { get; } = message;

    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;

    public bool RequiresForwarding => Level >= NotificationLevel.Warning;
}