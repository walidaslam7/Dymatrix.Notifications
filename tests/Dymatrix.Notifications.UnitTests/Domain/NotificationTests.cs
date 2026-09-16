using Dymatrix.Notifications.Domain.Constants;
using Dymatrix.Notifications.Domain.Models;

namespace Dymatrix.Notifications.UnitTests.Domain;

public sealed class NotificationTests
{
    [Theory]
    [InlineData(NotificationLevel.Info, false)]
    [InlineData(NotificationLevel.Warning, true)]
    [InlineData(NotificationLevel.Error, true)]
    [InlineData(NotificationLevel.Critical, true)]
    public void GivenNotificationLevel_WhenForwardingIsEvaluated_ThenReturnsExpectedResult(
        NotificationLevel level,
        bool expected)
    {
        var notification = new Notification(level, "A notification message");

        Assert.Equal(expected, notification.RequiresForwarding);
    }

}
