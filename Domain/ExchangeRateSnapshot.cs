namespace currency_converter.Domain;

public sealed class ExchangeRateSnapshot
{
    private readonly IReadOnlyDictionary<CurrencyCode, decimal> _rates;

    public DateOnly RateDate { get; }
    public DateTime LastRefreshUtc { get; }

    /// <summary>
    /// Rates are EUR-based. EUR itself is not in the dictionary (its rate is implicitly 1).
    /// </summary>
    public ExchangeRateSnapshot(
        DateOnly rateDate,
        IReadOnlyDictionary<CurrencyCode, decimal> rates,
        DateTime lastRefreshUtc)
    {
        RateDate = rateDate;
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        LastRefreshUtc = lastRefreshUtc;
    }

    /// <summary>
    /// A snapshot is considered stale if the rate date is more than 1 business day behind the current UTC date.
    /// Simple heuristic: stale if older than 3 calendar days (covers weekends).
    /// </summary>
    public bool IsStale => (DateTime.UtcNow.Date - RateDate.ToDateTime(TimeOnly.MinValue)).TotalDays > 3;

    public decimal GetRate(CurrencyCode currency)
    {
        if (currency.Value == "EUR")
            return 1m;

        if (_rates.TryGetValue(currency, out var rate))
            return rate;

        throw new CurrencyNotFoundException(currency);
    }

    public IReadOnlyList<CurrencyCode> GetSupportedCurrencies()
    {
        var list = new List<CurrencyCode>(_rates.Keys) { new("EUR") };
        list.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));
        return list;
    }
}
