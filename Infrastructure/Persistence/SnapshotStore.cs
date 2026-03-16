using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using currency_converter.Configuration;
using currency_converter.Domain;

namespace currency_converter.Infrastructure.Persistence;

public sealed class SnapshotStore
{
    private readonly string _filePath;
    private readonly ILogger<SnapshotStore> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SnapshotStore(IOptions<EcbOptions> options, ILogger<SnapshotStore> logger)
    {
        _filePath = options.Value.SnapshotFilePath;
        _logger = logger;
    }

    public async Task SaveAsync(ExchangeRateSnapshot snapshot)
    {
        var dto = new SnapshotJsonDto
        {
            RateDate = snapshot.RateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            LastRefreshUtc = snapshot.LastRefreshUtc,
            Rates = snapshot.GetSupportedCurrencies()
                .Where(c => c.Value != "EUR")
                .ToDictionary(c => c.Value, c => snapshot.GetRate(c))
        };

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);

        _logger.LogInformation("Snapshot saved to {Path} (RateDate={RateDate})", _filePath, dto.RateDate);
    }

    public async Task<ExchangeRateSnapshot?> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("No snapshot file found at {Path}", _filePath);
            return null;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        var dto = JsonSerializer.Deserialize<SnapshotJsonDto>(json);

        if (dto is null)
            return null;

        var rateDate = DateOnly.Parse(dto.RateDate, CultureInfo.InvariantCulture);
        var rates = dto.Rates.ToDictionary(
            kvp => new CurrencyCode(kvp.Key),
            kvp => kvp.Value);

        _logger.LogInformation("Snapshot loaded from {Path} (RateDate={RateDate})", _filePath, dto.RateDate);

        return new ExchangeRateSnapshot(rateDate, rates, dto.LastRefreshUtc);
    }
}
