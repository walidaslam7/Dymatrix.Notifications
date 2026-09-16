using Dymatrix.Notifications.Application.Abstractions;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini.Mapping;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Llm.Gemini;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Client;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord.Mapping;
using Dymatrix.Notifications.Infrastructure.ExternalProviders.Webhooks.Discord;
using Dymatrix.Notifications.Infrastructure.Processing;
using Dymatrix.Notifications.Infrastructure.Queue;
using Dymatrix.Notifications.Infrastructure.RateLimiting;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.Infrastructure.Extensions;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);
        AddQueue(services);
        AddGemini(services);
        AddDiscord(services);
        AddProcessing(services);

        return services;
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureQueueOptions(services, configuration);
        ConfigureRateLimitOptions(services, configuration);
        ConfigureGeminiOptions(services, configuration);
        ConfigureDiscordOptions(services, configuration);
    }

    private static void ConfigureQueueOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<NotificationQueueOptions>()
            .Bind(configuration.GetSection(NotificationQueueOptions.SectionName))
            .Validate(options => options.Capacity > 0, "Notification queue capacity must be greater than zero.")
            .ValidateOnStart();
    }

    private static void ConfigureRateLimitOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OutboundRateLimitOptions>()
            .Bind(configuration.GetSection(OutboundRateLimitOptions.SectionName))
            .Validate(options => options.PermitLimit > 0, "Outbound permit limit must be greater than zero.")
            .Validate(options => double.IsFinite(options.WindowSeconds) && options.WindowSeconds > 0,
                "Outbound rate-limit window must be a positive, finite number of seconds.")
            .ValidateOnStart();
    }

    private static void ConfigureGeminiOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "Gemini base URL must be a valid absolute URI.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Gemini API key must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "Gemini model must be configured.")
            .ValidateOnStart();
    }

    private static void ConfigureDiscordOptions(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DiscordOptions>()
            .Bind(configuration.GetSection(DiscordOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var webhookUri)
                           && webhookUri.Scheme == Uri.UriSchemeHttps,
                "Discord webhook URL must be a valid HTTPS URI.")
            .ValidateOnStart();
    }

    private static void AddQueue(IServiceCollection services)
    {
        services.AddSingleton<ChannelNotificationQueue>();
        services.AddSingleton<INotificationQueue>(provider =>
            provider.GetRequiredService<ChannelNotificationQueue>());
        services.AddSingleton<INotificationQueueReader>(provider =>
            provider.GetRequiredService<ChannelNotificationQueue>());
        services.AddSingleton<IOutboundNotificationRateLimiter, OutboundNotificationRateLimiter>();
    }

    private static void AddGemini(IServiceCollection services)
    {
        services.AddSingleton<GeminiRequestMapper>();
        services.AddSingleton<GeminiResponseMapper>();

        services.AddHttpClient(GeminiClient.ClientName, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddSingleton<GeminiClient>();
        services.AddSingleton<IMessageGenerator, GeminiMessageGenerator>();
    }

    private static void AddDiscord(IServiceCollection services)
    {
        services.AddSingleton<DiscordNotificationMapper>();
        services.AddHttpClient(DiscordWebhookClient.ClientName);
        services.AddSingleton<DiscordWebhookClient>();
        services.AddSingleton<INotificationPublisher, DiscordNotificationPublisher>();
    }

    private static void AddProcessing(IServiceCollection services)
    {
        services.AddSingleton<INotificationProcessor, NotificationProcessor>();
        services.AddHostedService<NotificationWorker>();
    }
}
