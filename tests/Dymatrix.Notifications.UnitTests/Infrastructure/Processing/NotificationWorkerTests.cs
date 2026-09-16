using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.Processing;
using Dymatrix.Notifications.Infrastructure.Queue;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.Processing;

public sealed class NotificationWorkerTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task GivenQueuedNotification_WhenWorkerRuns_ThenNotificationIsProcessed()
    {
        var queue = CreateQueue();
        var notification = CreateNotification("Process this");
        var processed = new TaskCompletionSource<Notification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = Substitute.For<INotificationProcessor>();
        processor
            .ProcessAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                processed.TrySetResult(callInfo.Arg<Notification>());
                return Task.CompletedTask;
            });
        using var worker = CreateWorker(queue, processor);
        await worker.StartAsync(CancellationToken.None);

        try
        {
            await queue.EnqueueAsync(notification, CancellationToken.None);

            var processedNotification = await processed.Task.WaitAsync(TestTimeout);

            Assert.Same(notification, processedNotification);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(TestTimeout);
        }
    }

    [Fact]
    public async Task GivenProcessingInProgress_WhenWorkerStops_ThenProcessingIsCancelledCleanly()
    {
        var queue = CreateQueue();
        var processingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = Substitute.For<INotificationProcessor>();
        processor
            .ProcessAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => WaitForCancellationAsync(
                callInfo.Arg<CancellationToken>(),
                processingStarted,
                cancellationObserved));
        using var worker = CreateWorker(queue, processor);
        await worker.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(CreateNotification("Cancel this"), CancellationToken.None);
        await processingStarted.Task.WaitAsync(TestTimeout);

        await worker.StopAsync(CancellationToken.None).WaitAsync(TestTimeout);

        await cancellationObserved.Task.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task GivenProcessorFailure_WhenWorkerContinues_ThenNextNotificationIsProcessed()
    {
        var queue = CreateQueue();
        var secondProcessed = new TaskCompletionSource<Notification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = Substitute.For<INotificationProcessor>();
        var invocationCount = 0;
        processor
            .ProcessAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                if (Interlocked.Increment(ref invocationCount) == 1)
                {
                    return Task.FromException(new InvalidOperationException("Processing failed."));
                }

                secondProcessed.TrySetResult(callInfo.Arg<Notification>());
                return Task.CompletedTask;
            });
        var first = CreateNotification("First");
        var second = CreateNotification("Second");
        using var worker = CreateWorker(queue, processor);
        await worker.StartAsync(CancellationToken.None);

        try
        {
            await queue.EnqueueAsync(first, CancellationToken.None);
            await queue.EnqueueAsync(second, CancellationToken.None);

            var processedNotification = await secondProcessed.Task.WaitAsync(TestTimeout);

            Assert.Same(second, processedNotification);
            await processor.Received(2).ProcessAsync(
                Arg.Any<Notification>(),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(TestTimeout);
        }
    }

    private static ChannelNotificationQueue CreateQueue() =>
        new(Options.Create(new NotificationQueueOptions { Capacity = 10 }));

    private static NotificationWorker CreateWorker(
        INotificationQueueReader queueReader,
        INotificationProcessor processor) =>
        new(queueReader, processor, NullLogger<NotificationWorker>.Instance);

    private static Notification CreateNotification(string message) =>
        new(NotificationLevel.Warning, message);

    private static async Task WaitForCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource processingStarted,
        TaskCompletionSource cancellationObserved)
    {
        processingStarted.TrySetResult();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationObserved.TrySetResult();
            throw;
        }
    }
}
