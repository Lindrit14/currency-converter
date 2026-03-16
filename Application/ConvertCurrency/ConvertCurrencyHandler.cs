using currency_converter.Contracts.Models;
using currency_converter.Domain;

namespace currency_converter.Application.ConvertCurrency;

public sealed class ConvertCurrencyHandler
{
    private readonly IRateSnapshotProvider _snapshotProvider;

    public ConvertCurrencyHandler(IRateSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public ConvertCurrencyResponse Handle(ConvertCurrencyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = _snapshotProvider.GetCurrentSnapshot()
            ?? throw new InvalidOperationException("Exchange rates are not available yet.");

        var from = new CurrencyCode(request.FromCurrency);
        var to = new CurrencyCode(request.ToCurrency);

        var result = ConversionService.Convert(request.Amount, from, to, snapshot);

        return new ConvertCurrencyResponse
        {
            OriginalAmount = result.OriginalAmount,
            ConvertedAmount = result.ConvertedAmount,
            ExchangeRate = result.ExchangeRate,
            FromCurrency = result.From.Value,
            ToCurrency = result.To.Value,
            RateDate = result.RateDate.ToDateTime(TimeOnly.MinValue),
            IsStale = result.IsStale
        };
    }
}
