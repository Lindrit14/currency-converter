using currency_converter.Domain;

namespace currency_converter.Tests.Domain;

public class ConversionServiceTests
{
    private static ExchangeRateSnapshot CreateSnapshot()
    {
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.1m,
            [new CurrencyCode("GBP")] = 0.85m,
            [new CurrencyCode("JPY")] = 160.0m,
        };

        return new ExchangeRateSnapshot(
            DateOnly.FromDateTime(DateTime.UtcNow),
            rates,
            DateTime.UtcNow);
    }

    [Fact]
    public void Convert_EurToUsd()
    {
        var result = ConversionService.Convert(100m, new("EUR"), new("USD"), CreateSnapshot());

        Assert.Equal(100m, result.OriginalAmount);
        Assert.Equal(110m, result.ConvertedAmount);
        Assert.Equal(new CurrencyCode("EUR"), result.From);
        Assert.Equal(new CurrencyCode("USD"), result.To);
    }

    [Fact]
    public void Convert_UsdToEur()
    {
        var result = ConversionService.Convert(110m, new("USD"), new("EUR"), CreateSnapshot());

        // 110 * (1 / 1.1) = 100
        Assert.Equal(100m, result.ConvertedAmount);
    }

    [Fact]
    public void Convert_JpyToGbp_CrossRate()
    {
        var result = ConversionService.Convert(16000m, new("JPY"), new("GBP"), CreateSnapshot());

        // 16000 * (0.85 / 160) = 85
        Assert.Equal(85m, result.ConvertedAmount);
    }

    [Fact]
    public void Convert_SameCurrency_ReturnsOriginal()
    {
        var result = ConversionService.Convert(42m, new("USD"), new("USD"), CreateSnapshot());

        Assert.Equal(42m, result.ConvertedAmount);
        Assert.Equal(1m, result.ExchangeRate);
    }

    [Fact]
    public void Convert_ZeroAmount_ReturnsZero()
    {
        var result = ConversionService.Convert(0m, new("EUR"), new("USD"), CreateSnapshot());
        Assert.Equal(0m, result.ConvertedAmount);
    }

    [Fact]
    public void Convert_NegativeAmount_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConversionService.Convert(-1m, new("EUR"), new("USD"), CreateSnapshot()));
    }

    [Fact]
    public void Convert_InvalidCurrency_ThrowsCurrencyNotFound()
    {
        Assert.Throws<CurrencyNotFoundException>(() =>
            ConversionService.Convert(100m, new("EUR"), new("XYZ"), CreateSnapshot()));
    }

    [Fact]
    public void Convert_ResultRoundedTo4Dp()
    {
        // Create a snapshot with a rate that produces many decimals
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.12345m,
            [new CurrencyCode("GBP")] = 0.87654m,
        };
        var snapshot = new ExchangeRateSnapshot(DateOnly.FromDateTime(DateTime.UtcNow), rates, DateTime.UtcNow);

        var result = ConversionService.Convert(100m, new("USD"), new("GBP"), snapshot);

        // Should be 4 decimal places max
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result.ConvertedAmount)[3])[2];
        Assert.True(decimalPlaces <= 4);
    }

    [Fact]
    public void Convert_NullSnapshot_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConversionService.Convert(100m, new("EUR"), new("USD"), null!));
    }
}
