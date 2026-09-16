using System.Threading.RateLimiting;
using Dymatrix.Notifications.Infrastructure.Abstractions;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.Infrastructure.RateLimiting;

public sealed class OutboundNotificationRateLimiter :
    IOutboundNotificationRateLimiter,
    IDisposable,
    IAsyncDisposable
{
    private const int SegmentsPerWindow = 10;
    private const int QueueLimit = 1;

    private readonly SlidingWindowRateLimiter _rateLimiter;

    public OutboundNotificationRateLimiter(IOptions<OutboundRateLimitOptions> options)
    {
        var settings = options.Value;

        _rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            SegmentsPerWindow = SegmentsPerWindow,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = QueueLimit,
            AutoReplenishment = true
        });
    }

    public async ValueTask AcquireAsync(CancellationToken cancellationToken)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("An outbound notification permit could not be acquired.");
        }
    }

    public void Dispose() => _rateLimiter.Dispose();

    public ValueTask DisposeAsync() => _rateLimiter.DisposeAsync();
}
