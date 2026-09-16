using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Queue;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.Queue;

public sealed class ChannelNotificationQueueTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task GivenNotification_WhenEnqueued_ThenItIsAvailableToReader()
    {
        var queue = CreateQueue(capacity: 1);
        var notification = CreateNotification("First");

        await queue.EnqueueAsync(notification, CancellationToken.None);
        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();

        Assert.True(await reader.MoveNextAsync());
        Assert.Same(notification, reader.Current);
    }

    [Fact]
    public async Task GivenMultipleNotifications_WhenRead_ThenFifoOrderingIsPreserved()
    {
        var queue = CreateQueue(capacity: 3);
        var notifications = new[]
        {
            CreateNotification("First"),
            CreateNotification("Second"),
            CreateNotification("Third")
        };

        foreach (var notification in notifications)
        {
            await queue.EnqueueAsync(notification, CancellationToken.None);
        }

        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        foreach (var expected in notifications)
        {
            Assert.True(await reader.MoveNextAsync());
            Assert.Same(expected, reader.Current);
        }
    }

    [Fact]
    public async Task GivenEmptyQueue_WhenReadingIsCancelled_ThenWaitingStops()
    {
        var queue = CreateQueue(capacity: 1);
        using var cancellationTokenSource = new CancellationTokenSource();
        await using var reader = queue
            .ReadAllAsync(cancellationTokenSource.Token)
            .GetAsyncEnumerator();
        var pendingRead = reader.MoveNextAsync().AsTask();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await pendingRead;
        });
    }

    [Fact]
    public async Task GivenFullQueue_WhenEnqueueIsCancelled_ThenCancellationIsHonored()
    {
        var queue = CreateQueue(capacity: 1);
        await queue.EnqueueAsync(CreateNotification("First"), CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        var pendingEnqueue = queue
            .EnqueueAsync(CreateNotification("Second"), cancellationTokenSource.Token)
            .AsTask();

        Assert.False(pendingEnqueue.IsCompleted);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await pendingEnqueue;
        });
    }

    [Fact]
    public async Task GivenFullQueue_WhenCapacityBecomesAvailable_ThenEnqueueCompletesWithoutDropping()
    {
        var queue = CreateQueue(capacity: 1);
        var first = CreateNotification("First");
        var second = CreateNotification("Second");
        await queue.EnqueueAsync(first, CancellationToken.None);

        var waitingEnqueue = queue.EnqueueAsync(second, CancellationToken.None).AsTask();

        Assert.False(waitingEnqueue.IsCompleted);

        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
        Assert.Same(first, reader.Current);

        await waitingEnqueue.WaitAsync(TestTimeout);
        Assert.True(await reader.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
        Assert.Same(second, reader.Current);
    }

    private static ChannelNotificationQueue CreateQueue(int capacity) =>
        new(Options.Create(new NotificationQueueOptions { Capacity = capacity }));

    private static Notification CreateNotification(string message) =>
        new(NotificationLevel.Warning, message);
}
