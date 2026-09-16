using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.Models;
using Dymatrix.Notifications.Infrastructure.Processing;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.Processing;

public sealed class NotificationProcessorTests
{
    [Fact]
    public async Task GivenNotification_WhenProcessed_ThenGeneratesLimitsAndPublishesInOrder()
    {
        var order = new List<string>();
        var notification = new Notification(NotificationLevel.Warning, "pool exhausted");
        var generated = new GeneratedMessage("database", "Connection pool exhausted.");
        var generator = new RecordingGenerator(order, generated);
        var limiter = new RecordingLimiter(order);
        var publisher = new RecordingPublisher(order);
        var processor = new NotificationProcessor(generator, limiter, publisher);

        await processor.ProcessAsync(notification, CancellationToken.None);

        Assert.Equal(new[] { "generate", "limit", "publish" }, order);
        Assert.Same(notification, publisher.Notification);
        Assert.Same(generated, publisher.GeneratedMessage);
    }

    [Fact]
    public async Task GivenGenerationFailure_WhenProcessed_ThenDoesNotLimitOrPublish()
    {
        var generator = new FailingGenerator(new InvalidOperationException("generation failed"));
        var limiter = new RecordingLimiter([]);
        var publisher = new RecordingPublisher([]);
        var processor = new NotificationProcessor(generator, limiter, publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(new Notification(NotificationLevel.Error, "failed"), CancellationToken.None));

        Assert.Equal(0, limiter.Calls);
        Assert.Equal(0, publisher.Calls);
    }

    [Fact]
    public async Task GivenRateLimitFailure_WhenProcessed_ThenDoesNotPublish()
    {
        var limiter = new FailingLimiter(new InvalidOperationException("limit failed"));
        var publisher = new RecordingPublisher([]);
        var processor = new NotificationProcessor(
            new RecordingGenerator([], new GeneratedMessage("availability", "Unavailable.")),
            limiter,
            publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(new Notification(NotificationLevel.Error, "failed"), CancellationToken.None));

        Assert.Equal(0, publisher.Calls);
    }

    [Fact]
    public async Task GivenPublisherFailure_WhenProcessed_ThenFailureIsPropagated()
    {
        var expected = new InvalidOperationException("publish failed");
        var processor = new NotificationProcessor(
            new RecordingGenerator([], new GeneratedMessage("network", "Network unavailable.")),
            new RecordingLimiter([]),
            new FailingPublisher(expected));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(new Notification(NotificationLevel.Error, "failed"), CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [Fact]
    public async Task GivenCancellation_WhenProcessed_ThenCancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var processor = new NotificationProcessor(
            new RecordingGenerator([], new GeneratedMessage("availability", "Unavailable.")),
            new CancellingLimiter(),
            new RecordingPublisher([]));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(new Notification(NotificationLevel.Error, "cancelled"), cancellation.Token));
    }

    private sealed class RecordingGenerator(List<string> order, GeneratedMessage result) : IMessageGenerator
    {
        public Task<GeneratedMessage> GenerateAsync(Notification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            order.Add("generate");
            return Task.FromResult(result);
        }
    }

    private sealed class FailingGenerator(Exception exception) : IMessageGenerator
    {
        public Task<GeneratedMessage> GenerateAsync(Notification notification, CancellationToken cancellationToken) =>
            Task.FromException<GeneratedMessage>(exception);
    }

    private sealed class RecordingLimiter(List<string> order) : IOutboundNotificationRateLimiter
    {
        public int Calls { get; private set; }

        public ValueTask AcquireAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            order.Add("limit");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingLimiter(Exception exception) : IOutboundNotificationRateLimiter
    {
        public ValueTask AcquireAsync(CancellationToken cancellationToken) =>
            ValueTask.FromException(exception);
    }

    private sealed class CancellingLimiter : IOutboundNotificationRateLimiter
    {
        public ValueTask AcquireAsync(CancellationToken cancellationToken) =>
            ValueTask.FromCanceled(cancellationToken);
    }

    private sealed class RecordingPublisher(List<string> order) : INotificationPublisher
    {
        public int Calls { get; private set; }
        public Notification? Notification { get; private set; }
        public GeneratedMessage? GeneratedMessage { get; private set; }

        public Task PublishAsync(Notification notification, GeneratedMessage generatedMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Notification = notification;
            GeneratedMessage = generatedMessage;
            order.Add("publish");
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublisher(Exception exception) : INotificationPublisher
    {
        public Task PublishAsync(Notification notification, GeneratedMessage generatedMessage, CancellationToken cancellationToken) =>
            Task.FromException(exception);
    }
}
