using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dymatrix.Notifications.Api.Extensions;

public static class ConfigureServices
{
    public static IServiceCollection AddApiJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.RespectRequiredConstructorParameters = true;
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(
                    JsonNamingPolicy.CamelCase,
                    allowIntegerValues: false));
        });

        return services;
    }
}
