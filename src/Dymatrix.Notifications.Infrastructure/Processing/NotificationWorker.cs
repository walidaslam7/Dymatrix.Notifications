using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dymatrix.Notifications.Infrastructure.Processing;

public sealed partial class NotificationWorker(
    INotificationQueueReader queueReader,
    INotificationProcessor processor,
    ILogger<NotificationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in queueReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                ProcessingNotification(logger, notification.Id, notification.Level);
                await processor.ProcessAsync(notification, stoppingToken);
                ProcessedNotification(logger, notification.Id, notification.Level);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                FailedToProcessNotification(logger, exception, notification.Id, notification.Level);
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing notification {NotificationId} at level {NotificationLevel}")]
    private static partial void ProcessingNotification(ILogger logger, Guid notificationId, NotificationLevel notificationLevel);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Processed notification {NotificationId} at level {NotificationLevel}")]
    private static partial void ProcessedNotification(ILogger logger, Guid notificationId, NotificationLevel notificationLevel);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to process notification {NotificationId} at level {NotificationLevel}")]
    private static partial void FailedToProcessNotification(ILogger logger, Exception exception, Guid notificationId, NotificationLevel notificationLevel);
}
