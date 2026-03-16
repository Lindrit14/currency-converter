using currency_converter.Domain;

namespace currency_converter.Application;

public interface IRateSnapshotProvider
{
    ExchangeRateSnapshot? GetCurrentSnapshot();
}
