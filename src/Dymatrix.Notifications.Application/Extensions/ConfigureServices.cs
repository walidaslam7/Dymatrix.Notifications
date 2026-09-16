using Dymatrix.Notifications.Application.Notifications.Submit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Dymatrix.Notifications.Application.Extensions;

public static class ConfigureServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SubmitNotificationHandler>();
        services.AddSingleton<IValidator<SubmitNotificationCommand>, SubmitNotificationValidator>();

        return services;
    }
}
