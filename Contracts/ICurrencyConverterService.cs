using CoreWCF;
using currency_converter.Contracts.Models;

namespace currency_converter.Contracts;

[ServiceContract(Namespace = "http://currencyconverter.local/")]
public interface ICurrencyConverterService
{
    [OperationContract]
    ConvertCurrencyResponse ConvertCurrency(ConvertCurrencyRequest request);

    [OperationContract]
    GetSupportedCurrenciesResponse GetSupportedCurrencies();

    [OperationContract]
    GetRateMetadataResponse GetRateMetadata();
}
