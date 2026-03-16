using System.Runtime.Serialization;

namespace currency_converter.Contracts.Models;

[DataContract(Namespace = "http://currencyconverter.local/models")]
public class ConvertCurrencyRequest
{
    [DataMember(Order = 1)]
    public decimal Amount { get; set; }

    [DataMember(Order = 2)]
    public string FromCurrency { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string ToCurrency { get; set; } = string.Empty;
}
