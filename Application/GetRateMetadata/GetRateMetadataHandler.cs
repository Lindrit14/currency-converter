using currency_converter.Contracts.Models;

namespace currency_converter.Application.GetRateMetadata;

public sealed class GetRateMetadataHandler
{
    private readonly IRateSnapshotProvider _snapshotProvider;

    public GetRateMetadataHandler(IRateSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public GetRateMetadataResponse Handle()
    {
        var snapshot = _snapshotProvider.GetCurrentSnapshot()
            ?? throw new InvalidOperationException("Exchange rates are not available yet.");

        return new GetRateMetadataResponse
        {
            RateDate = snapshot.RateDate.ToDateTime(TimeOnly.MinValue),
            Source = "ECB",
            LastRefreshUtc = snapshot.LastRefreshUtc,
            IsStale = snapshot.IsStale
        };
    }
}
