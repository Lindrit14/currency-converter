using currency_converter.Application;
using currency_converter.Domain;

namespace currency_converter.Infrastructure.Persistence;

public sealed class RateSnapshotCache : IRateSnapshotProvider
{
    private volatile ExchangeRateSnapshot? _current;
    private readonly object _lock = new();

    public ExchangeRateSnapshot? GetCurrentSnapshot() => _current;

    public void Update(ExchangeRateSnapshot snapshot)
    {
        lock (_lock)
        {
            _current = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }
    }
}
