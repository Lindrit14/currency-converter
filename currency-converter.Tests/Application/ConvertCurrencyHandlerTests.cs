using currency_converter.Application;
using currency_converter.Application.ConvertCurrency;
using currency_converter.Contracts.Models;
using currency_converter.Domain;

namespace currency_converter.Tests.Application;

public class ConvertCurrencyHandlerTests
{
    private static ExchangeRateSnapshot CreateSnapshot()
    {
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.1m,
            [new CurrencyCode("GBP")] = 0.85m,
        };

        return new ExchangeRateSnapshot(
            DateOnly.FromDateTime(DateTime.UtcNow),
            rates,
            DateTime.UtcNow);
    }

    private sealed class FakeSnapshotProvider : IRateSnapshotProvider
    {
        private readonly ExchangeRateSnapshot? _snapshot;
        public FakeSnapshotProvider(ExchangeRateSnapshot? snapshot) => _snapshot = snapshot;
        public ExchangeRateSnapshot? GetCurrentSnapshot() => _snapshot;
    }

    [Fact]
    public void Handle_ValidRequest_ReturnsResponse()
    {
        var handler = new ConvertCurrencyHandler(new FakeSnapshotProvider(CreateSnapshot()));

        var response = handler.Handle(new ConvertCurrencyRequest
        {
            Amount = 100,
            FromCurrency = "EUR",
            ToCurrency = "USD"
        });

        Assert.Equal(100m, response.OriginalAmount);
        Assert.Equal(110m, response.ConvertedAmount);
        Assert.Equal("EUR", response.FromCurrency);
        Assert.Equal("USD", response.ToCurrency);
    }

    [Fact]
    public void Handle_NoSnapshot_ThrowsInvalidOperation()
    {
        var handler = new ConvertCurrencyHandler(new FakeSnapshotProvider(null));

        Assert.Throws<InvalidOperationException>(() =>
            handler.Handle(new ConvertCurrencyRequest
            {
                Amount = 100,
                FromCurrency = "EUR",
                ToCurrency = "USD"
            }));
    }

    [Fact]
    public void Handle_InvalidCurrency_ThrowsArgumentException()
    {
        var handler = new ConvertCurrencyHandler(new FakeSnapshotProvider(CreateSnapshot()));

        Assert.Throws<ArgumentException>(() =>
            handler.Handle(new ConvertCurrencyRequest
            {
                Amount = 100,
                FromCurrency = "INVALID",
                ToCurrency = "USD"
            }));
    }
}
