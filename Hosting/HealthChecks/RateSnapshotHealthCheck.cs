using Microsoft.Extensions.Diagnostics.HealthChecks;
using currency_converter.Application;

namespace currency_converter.Hosting.HealthChecks;

public sealed class RateSnapshotHealthCheck : IHealthCheck
{
    private readonly IRateSnapshotProvider _snapshotProvider;

    public RateSnapshotHealthCheck(IRateSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = _snapshotProvider.GetCurrentSnapshot();

        if (snapshot is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "No exchange rate snapshot available."));
        }

        if (snapshot.IsStale)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Exchange rate snapshot is stale (RateDate={snapshot.RateDate})."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Exchange rates loaded (RateDate={snapshot.RateDate}, LastRefresh={snapshot.LastRefreshUtc:u})."));
    }
}
