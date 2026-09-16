namespace Dymatrix.Notifications.Infrastructure.Settings;

public sealed class OutboundRateLimitOptions
{
    public const string SectionName = "OutboundRateLimit";

    public int PermitLimit { get; init; } = 10;

    public double WindowSeconds { get; init; } = 60;
}
