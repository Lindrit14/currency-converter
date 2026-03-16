using System.Runtime.Serialization;

namespace currency_converter.Contracts.Models;

[DataContract(Namespace = "http://currencyconverter.local/models")]
public class GetRateMetadataResponse
{
    [DataMember(Order = 1)]
    public DateTime RateDate { get; set; }

    [DataMember(Order = 2)]
    public string Source { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public DateTime LastRefreshUtc { get; set; }

    [DataMember(Order = 4)]
    public bool IsStale { get; set; }
}
