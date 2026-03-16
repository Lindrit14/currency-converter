namespace currency_converter.Domain;

public static class ConversionService
{
    public static ConversionResult Convert(
        decimal amount,
        CurrencyCode from,
        CurrencyCode to,
        ExchangeRateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must not be negative.");

        var fromRate = snapshot.GetRate(from);
        var toRate = snapshot.GetRate(to);

        var exchangeRate = toRate / fromRate;
        var convertedAmount = Math.Round(amount * exchangeRate, 4, MidpointRounding.AwayFromZero);

        return new ConversionResult(
            OriginalAmount: amount,
            ConvertedAmount: convertedAmount,
            ExchangeRate: Math.Round(exchangeRate, 6, MidpointRounding.AwayFromZero),
            From: from,
            To: to,
            RateDate: snapshot.RateDate,
            IsStale: snapshot.IsStale);
    }
}
