namespace Dymatrix.Notifications.Infrastructure.Settings;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string? WebhookUrl { get; init; }
}
