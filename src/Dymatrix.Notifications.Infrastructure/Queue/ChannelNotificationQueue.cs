using System.Threading.Channels;
using Dymatrix.Notifications.Application.Abstractions;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.Infrastructure.Queue;

public sealed class ChannelNotificationQueue : INotificationQueue, INotificationQueueReader
{
    private readonly Channel<Notification> _channel;

    public ChannelNotificationQueue(IOptions<NotificationQueueOptions> options)
    {
        var capacity = options.Value.Capacity;
        _channel = Channel.CreateBounded<Notification>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(Notification notification, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(notification, cancellationToken);

    public IAsyncEnumerable<Notification> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}