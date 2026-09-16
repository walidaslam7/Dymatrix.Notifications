using Dymatrix.Notifications.Application.Notifications.Submit;
using Dymatrix.Notifications.Domain.Constants;

namespace Dymatrix.Notifications.UnitTests.Application;

public sealed class SubmitNotificationValidatorTests
{
    private readonly SubmitNotificationValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenMissingOrWhitespaceMessage_WhenValidated_ThenFails(string? message)
    {
        var result = await _validator.ValidateAsync(new SubmitNotificationCommand(NotificationLevel.Info, message!));

        Assert.Contains(result.Errors, error => error.PropertyName == "Message");
    }

    [Fact]
    public async Task GivenUndefinedLevel_WhenValidated_ThenFails()
    {
        var result = await _validator.ValidateAsync(new SubmitNotificationCommand((NotificationLevel)999, "Message"));

        Assert.Contains(result.Errors, error => error.PropertyName == "Level");
    }
}
