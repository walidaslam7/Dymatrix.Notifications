using Dymatrix.Notifications.Api.Contracts.Notifications;
using Dymatrix.Notifications.Application.Notifications.Submit;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dymatrix.Notifications.Api.Endpoints.Notifications;

public static class SubmitNotificationEndpoint
{
    public static IEndpointRouteBuilder MapSubmitNotificationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/notifications", HandleAsync)
            .WithName("SubmitNotification")
            .WithTags("Notifications")
            .WithSummary("Submit a notification")
            .WithDescription("Accepts an informational notification immediately or queues warning, error, and critical notifications for asynchronous processing.")
            .Produces<SubmitNotificationResponse>(StatusCodes.Status200OK)
            .Produces<SubmitNotificationResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<Ok<SubmitNotificationResponse>, Accepted<SubmitNotificationResponse>, ValidationProblem>> HandleAsync(
        SubmitNotificationRequest request,
        SubmitNotificationHandler handler,
        IValidator<SubmitNotificationCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new SubmitNotificationCommand(request.Level, request.Message);
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        var response = new SubmitNotificationResponse(result.NotificationId, result.Queued);

        return ToHttpResult(result, response);
    }

    private static Results<Ok<SubmitNotificationResponse>, Accepted<SubmitNotificationResponse>, ValidationProblem> ToHttpResult(
        SubmitNotificationOutcome outcome,
        SubmitNotificationResponse response)
    {
        return outcome.Queued
            ? TypedResults.Accepted(uri: (string?)null, value: response)
            : TypedResults.Ok(response);
    }
}