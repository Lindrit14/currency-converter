namespace currency_converter.Infrastructure.Persistence;

public sealed class SnapshotJsonDto
{
    public string RateDate { get; set; } = string.Empty;
    public DateTime LastRefreshUtc { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
