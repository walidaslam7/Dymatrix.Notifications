using FluentValidation;

namespace Dymatrix.Notifications.Application.Notifications.Submit;

public sealed class SubmitNotificationValidator : AbstractValidator<SubmitNotificationCommand>
{
    public SubmitNotificationValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .WithMessage("Message is required.");

        RuleFor(command => command.Level)
            .IsInEnum()
            .WithMessage("Level must be one of Info, Warning, Error, or Critical.");
    }
}
