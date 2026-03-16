using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using currency_converter.Configuration;
using currency_converter.Domain;
using currency_converter.Infrastructure.Persistence;

namespace currency_converter.Tests.Infrastructure;

public class SnapshotStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SnapshotStore _store;

    public SnapshotStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "currency_converter_tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var filePath = Path.Combine(_tempDir, "snapshot.json");
        var options = Options.Create(new EcbOptions { SnapshotFilePath = filePath });
        _store = new SnapshotStore(options, NullLogger<SnapshotStore>.Instance);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.1m,
            [new CurrencyCode("GBP")] = 0.85m,
        };
        var original = new ExchangeRateSnapshot(
            new DateOnly(2024, 12, 20), rates, new DateTime(2024, 12, 20, 16, 0, 0, DateTimeKind.Utc));

        await _store.SaveAsync(original);
        var loaded = await _store.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(original.RateDate, loaded.RateDate);
        Assert.Equal(original.LastRefreshUtc, loaded.LastRefreshUtc);
        Assert.Equal(original.GetRate(new CurrencyCode("USD")), loaded.GetRate(new CurrencyCode("USD")));
        Assert.Equal(original.GetRate(new CurrencyCode("GBP")), loaded.GetRate(new CurrencyCode("GBP")));
    }

    [Fact]
    public async Task Load_MissingFile_ReturnsNull()
    {
        var result = await _store.LoadAsync();
        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
