using currency_converter.Domain;

namespace currency_converter.Tests.Domain;

public class ExchangeRateSnapshotTests
{
    private static ExchangeRateSnapshot CreateSnapshot(DateOnly? rateDate = null)
    {
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.1m,
            [new CurrencyCode("GBP")] = 0.85m,
            [new CurrencyCode("JPY")] = 160.0m,
        };

        return new ExchangeRateSnapshot(
            rateDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            rates,
            DateTime.UtcNow);
    }

    [Fact]
    public void GetRate_EUR_ReturnsOne()
    {
        var snapshot = CreateSnapshot();
        Assert.Equal(1m, snapshot.GetRate(new CurrencyCode("EUR")));
    }

    [Fact]
    public void GetRate_USD_ReturnsConfiguredRate()
    {
        var snapshot = CreateSnapshot();
        Assert.Equal(1.1m, snapshot.GetRate(new CurrencyCode("USD")));
    }

    [Fact]
    public void GetRate_UnknownCurrency_ThrowsCurrencyNotFoundException()
    {
        var snapshot = CreateSnapshot();
        Assert.Throws<CurrencyNotFoundException>(() => snapshot.GetRate(new CurrencyCode("XYZ")));
    }

    [Fact]
    public void GetSupportedCurrencies_IncludesEurAndAll()
    {
        var snapshot = CreateSnapshot();
        var currencies = snapshot.GetSupportedCurrencies().Select(c => c.Value).ToList();

        Assert.Contains("EUR", currencies);
        Assert.Contains("USD", currencies);
        Assert.Contains("GBP", currencies);
        Assert.Contains("JPY", currencies);
        Assert.Equal(4, currencies.Count);
    }

    [Fact]
    public void GetSupportedCurrencies_IsSorted()
    {
        var snapshot = CreateSnapshot();
        var currencies = snapshot.GetSupportedCurrencies().Select(c => c.Value).ToList();
        var sorted = currencies.OrderBy(c => c).ToList();
        Assert.Equal(sorted, currencies);
    }

    [Fact]
    public void IsStale_RecentDate_ReturnsFalse()
    {
        var snapshot = CreateSnapshot(DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.False(snapshot.IsStale);
    }

    [Fact]
    public void IsStale_OldDate_ReturnsTrue()
    {
        var snapshot = CreateSnapshot(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        Assert.True(snapshot.IsStale);
    }
}
