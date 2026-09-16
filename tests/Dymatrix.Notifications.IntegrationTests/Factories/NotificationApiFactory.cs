using System.Threading.Channels;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dymatrix.Notifications.IntegrationTests.Factories;

public sealed class NotificationApiFactory : WebApplicationFactory<Program>
{
    private readonly Channel<Notification> _processedNotifications = Channel.CreateUnbounded<Notification>();

    public async Task<Notification> WaitForProcessedAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        while (await _processedNotifications.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_processedNotifications.Reader.TryRead(out var notification))
            {
                if (notification.Id == notificationId)
                {
                    return notification;
                }
            }
        }

        throw new InvalidOperationException("The processed-notification channel completed unexpectedly.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<INotificationProcessor>();
            services.AddSingleton<INotificationProcessor>(new RecordingNotificationProcessor(_processedNotifications.Writer));
        });
    }

    private sealed class RecordingNotificationProcessor(ChannelWriter<Notification> processedNotifications) : INotificationProcessor
    {
        public Task ProcessAsync(Notification notification, CancellationToken cancellationToken)
        {
            processedNotifications.TryWrite(notification);
            return Task.CompletedTask;
        }
    }
}
