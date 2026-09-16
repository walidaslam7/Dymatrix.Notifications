using Dymatrix.Notifications.Application.Abstractions;
using Dymatrix.Notifications.Application.Notifications.Submit;
using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;
using NSubstitute;

namespace Dymatrix.Notifications.UnitTests.Application;

public sealed class SubmitNotificationHandlerTests
{
    [Fact]
    public async Task GivenInfoNotification_WhenHandled_ThenDoesNotEnqueueNotification()
    {
        var queue = Substitute.For<INotificationQueue>();
        var handler = new SubmitNotificationHandler(queue);
        var command = new SubmitNotificationCommand(NotificationLevel.Info, "For information only");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.NotificationId);
        Assert.False(result.Queued);
        await queue.DidNotReceive().EnqueueAsync(
            Arg.Any<Notification>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(NotificationLevel.Warning)]
    [InlineData(NotificationLevel.Error)]
    [InlineData(NotificationLevel.Critical)]
    public async Task GivenForwardingNotification_WhenHandled_ThenEnqueuesNotification(
        NotificationLevel level)
    {
        const string message = "Forward this notification";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var queue = Substitute.For<INotificationQueue>();
        queue.EnqueueAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        var handler = new SubmitNotificationHandler(queue);
        var command = new SubmitNotificationCommand(level, message);

        var result = await handler.HandleAsync(command, cancellationToken);

        Assert.NotEqual(Guid.Empty, result.NotificationId);
        Assert.True(result.Queued);
        await queue.Received(1).EnqueueAsync(
            Arg.Is<Notification>(notification =>
                notification.Id == result.NotificationId &&
                notification.Level == level &&
                notification.Message == message &&
                notification.RequiresForwarding),
            cancellationToken);
    }
}
