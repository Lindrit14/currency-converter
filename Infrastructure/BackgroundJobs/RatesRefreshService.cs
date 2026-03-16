using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using currency_converter.Configuration;
using currency_converter.Infrastructure.Ecb;
using currency_converter.Infrastructure.Persistence;

namespace currency_converter.Infrastructure.BackgroundJobs;

public sealed class RatesRefreshService : BackgroundService
{
    private readonly EcbClient _ecbClient;
    private readonly SnapshotStore _snapshotStore;
    private readonly RateSnapshotCache _cache;
    private readonly EcbOptions _options;
    private readonly ILogger<RatesRefreshService> _logger;

    public RatesRefreshService(
        EcbClient ecbClient,
        SnapshotStore snapshotStore,
        RateSnapshotCache cache,
        IOptions<EcbOptions> options,
        ILogger<RatesRefreshService> logger)
    {
        _ecbClient = ecbClient;
        _snapshotStore = snapshotStore;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Try loading persisted snapshot from disk
        await LoadFromDiskAsync();

        // 2. Fetch live rates immediately on startup
        await FetchAndUpdateAsync(stoppingToken);

        // 3. Loop: sleep until next scheduled refresh, then fetch
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRefresh();
            _logger.LogInformation("Next ECB rate refresh in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await FetchAndUpdateAsync(stoppingToken);
        }
    }

    private async Task LoadFromDiskAsync()
    {
        try
        {
            var snapshot = await _snapshotStore.LoadAsync();
            if (snapshot is not null)
            {
                _cache.Update(snapshot);
                _logger.LogInformation("Loaded persisted snapshot (RateDate={RateDate})", snapshot.RateDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted snapshot from disk");
        }
    }

    private async Task FetchAndUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var xml = await _ecbClient.FetchRatesXmlAsync(cancellationToken);
            var snapshot = EcbXmlParser.Parse(xml, DateTime.UtcNow);
            _cache.Update(snapshot);
            await _snapshotStore.SaveAsync(snapshot);
            _logger.LogInformation("ECB rates refreshed successfully (RateDate={RateDate})", snapshot.RateDate);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch ECB rates. Keeping last successful snapshot.");
        }
    }

    private TimeSpan ComputeDelayUntilNextRefresh()
    {
        var cetZone = GetCetTimeZone();
        var nowCet = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cetZone);
        var targetToday = new DateTime(nowCet.Year, nowCet.Month, nowCet.Day,
            _options.RefreshHourCet, _options.RefreshMinuteCet, 0);

        var target = nowCet < targetToday ? targetToday : targetToday.AddDays(1);
        var targetUtc = TimeZoneInfo.ConvertTimeToUtc(target, cetZone);

        return targetUtc - DateTime.UtcNow;
    }

    private static TimeZoneInfo GetCetTimeZone()
    {
        // Windows uses "Central European Standard Time", Linux/macOS use IANA "Europe/Berlin"
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
    }
}
