using Dymatrix.Notifications.Infrastructure.RateLimiting;
using Dymatrix.Notifications.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Dymatrix.Notifications.UnitTests.Infrastructure.RateLimiting;

public sealed class OutboundNotificationRateLimiterTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task GivenAvailablePermit_WhenAcquired_ThenCompletesImmediately()
    {
        await using var rateLimiter = CreateRateLimiter(permitLimit: 2, windowSeconds: 60);

        var firstAcquisition = rateLimiter.AcquireAsync(CancellationToken.None);
        var secondAcquisition = rateLimiter.AcquireAsync(CancellationToken.None);

        Assert.True(firstAcquisition.IsCompletedSuccessfully);
        Assert.True(secondAcquisition.IsCompletedSuccessfully);
        await firstAcquisition;
        await secondAcquisition;
    }

    [Fact]
    public async Task GivenExhaustedPermitLimit_WhenReplenished_ThenWaitingAcquisitionProceeds()
    {
        await using var rateLimiter = CreateRateLimiter(permitLimit: 1, windowSeconds: 0.2);
        await rateLimiter.AcquireAsync(CancellationToken.None);

        var waitingAcquisition = rateLimiter.AcquireAsync(CancellationToken.None).AsTask();

        Assert.False(waitingAcquisition.IsCompleted);
        await waitingAcquisition.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task GivenWaitingAcquisition_WhenCancelled_ThenCancellationIsHonored()
    {
        await using var rateLimiter = CreateRateLimiter(permitLimit: 1, windowSeconds: 60);
        await rateLimiter.AcquireAsync(CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        var waitingAcquisition = rateLimiter.AcquireAsync(cancellationTokenSource.Token).AsTask();

        Assert.False(waitingAcquisition.IsCompleted);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await waitingAcquisition;
        });
    }

    [Fact]
    public async Task GivenFullLimiterQueue_WhenPermitIsAcquired_ThenFailedLeaseThrowsExplicitly()
    {
        await using var rateLimiter = CreateRateLimiter(permitLimit: 1, windowSeconds: 60);
        await rateLimiter.AcquireAsync(CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        var waitingAcquisition = rateLimiter.AcquireAsync(cancellationTokenSource.Token).AsTask();

        var rejectedAcquisition = rateLimiter.AcquireAsync(CancellationToken.None).AsTask();

        await Assert.ThrowsAsync<InvalidOperationException>(() => rejectedAcquisition);
        cancellationTokenSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await waitingAcquisition;
        });
    }

    private static OutboundNotificationRateLimiter CreateRateLimiter(
        int permitLimit,
        double windowSeconds) =>
        new(Options.Create(new OutboundRateLimitOptions
        {
            PermitLimit = permitLimit,
            WindowSeconds = windowSeconds
        }));

}
