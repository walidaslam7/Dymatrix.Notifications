using Dymatrix.Notifications.Api.Endpoints.Notifications;
using Dymatrix.Notifications.Api.ErrorHandling;
using Dymatrix.Notifications.Api.Extensions;
using Dymatrix.Notifications.Application.Extensions;
using Dymatrix.Notifications.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddApiJsonOptions();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health");
app.MapSubmitNotificationEndpoint();

app.Run();

public abstract partial class Program;
