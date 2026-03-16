using System.Runtime.Serialization;

namespace currency_converter.Contracts.Models;

[DataContract(Namespace = "http://currencyconverter.local/models")]
public class ConvertCurrencyResponse
{
    [DataMember(Order = 1)]
    public decimal OriginalAmount { get; set; }

    [DataMember(Order = 2)]
    public decimal ConvertedAmount { get; set; }

    [DataMember(Order = 3)]
    public decimal ExchangeRate { get; set; }

    [DataMember(Order = 4)]
    public string FromCurrency { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string ToCurrency { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public DateTime RateDate { get; set; }

    [DataMember(Order = 7)]
    public bool IsStale { get; set; }
}
