namespace currency_converter.Configuration;

public sealed class EcbOptions
{
    public const string SectionName = "Ecb";

    public string FeedUrl { get; set; } = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    public int RefreshHourCet { get; set; } = 16;
    public int RefreshMinuteCet { get; set; } = 15;
    public string SnapshotFilePath { get; set; } = "Data/snapshot.json";
}
