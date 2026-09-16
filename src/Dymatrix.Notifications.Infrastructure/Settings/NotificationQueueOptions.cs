namespace Dymatrix.Notifications.Infrastructure.Settings;

public sealed class NotificationQueueOptions
{
    public const string SectionName = "NotificationQueue";

    public int Capacity { get; init; } = 100;
}
