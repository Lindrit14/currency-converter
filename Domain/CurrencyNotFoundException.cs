namespace currency_converter.Domain;

public sealed class CurrencyNotFoundException : Exception
{
    public CurrencyCode Currency { get; }

    public CurrencyNotFoundException(CurrencyCode currency)
        : base($"Currency '{currency.Value}' is not supported.")
    {
        Currency = currency;
    }
}
