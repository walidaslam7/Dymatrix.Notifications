using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Dymatrix.Notifications.Api.ErrorHandling;

public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested:
                return false;
            case BadHttpRequestException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "The request is invalid.",
                        Detail = "The request body or syntax could not be understood."
                    }
                });

                return true;
        }

        UnhandledException(logger, exception,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server could not complete the request."
        };

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        return true;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception while processing {RequestMethod} {RequestPath}")]
    private static partial void UnhandledException(ILogger logger, Exception exception, string requestMethod, string requestPath);
}
