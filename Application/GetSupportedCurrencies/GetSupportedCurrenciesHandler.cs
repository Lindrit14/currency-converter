using currency_converter.Contracts.Models;

namespace currency_converter.Application.GetSupportedCurrencies;

public sealed class GetSupportedCurrenciesHandler
{
    private readonly IRateSnapshotProvider _snapshotProvider;

    public GetSupportedCurrenciesHandler(IRateSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public GetSupportedCurrenciesResponse Handle()
    {
        var snapshot = _snapshotProvider.GetCurrentSnapshot()
            ?? throw new InvalidOperationException("Exchange rates are not available yet.");

        return new GetSupportedCurrenciesResponse
        {
            CurrencyCodes = snapshot.GetSupportedCurrencies().Select(c => c.Value).ToArray(),
            RateDate = snapshot.RateDate.ToDateTime(TimeOnly.MinValue)
        };
    }
}
