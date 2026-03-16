using System.Runtime.Serialization;

namespace currency_converter.Contracts.Models;

[DataContract(Namespace = "http://currencyconverter.local/models")]
public class GetSupportedCurrenciesResponse
{
    [DataMember(Order = 1)]
    public string[] CurrencyCodes { get; set; } = [];

    [DataMember(Order = 2)]
    public DateTime RateDate { get; set; }
}
