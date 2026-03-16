namespace currency_converter.Domain;

public sealed record ConversionResult(
    decimal OriginalAmount,
    decimal ConvertedAmount,
    decimal ExchangeRate,
    CurrencyCode From,
    CurrencyCode To,
    DateOnly RateDate,
    bool IsStale);
