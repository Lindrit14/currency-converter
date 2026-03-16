using Microsoft.Extensions.Logging;
using currency_converter.Application.ConvertCurrency;
using currency_converter.Application.GetRateMetadata;
using currency_converter.Application.GetSupportedCurrencies;
using currency_converter.Contracts;
using currency_converter.Contracts.Models;

namespace currency_converter.Hosting.Soap;

public sealed class CurrencyConverterSoapService : ICurrencyConverterService
{
    private readonly ConvertCurrencyHandler _convertHandler;
    private readonly GetSupportedCurrenciesHandler _currenciesHandler;
    private readonly GetRateMetadataHandler _metadataHandler;
    private readonly ILogger<CurrencyConverterSoapService> _logger;

    public CurrencyConverterSoapService(
        ConvertCurrencyHandler convertHandler,
        GetSupportedCurrenciesHandler currenciesHandler,
        GetRateMetadataHandler metadataHandler,
        ILogger<CurrencyConverterSoapService> logger)
    {
        _convertHandler = convertHandler;
        _currenciesHandler = currenciesHandler;
        _metadataHandler = metadataHandler;
        _logger = logger;
    }

    public ConvertCurrencyResponse ConvertCurrency(ConvertCurrencyRequest request)
    {
        try
        {
            _logger.LogInformation("ConvertCurrency: {Amount} {From} -> {To}",
                request.Amount, request.FromCurrency, request.ToCurrency);
            return _convertHandler.Handle(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConvertCurrency failed");
            throw SoapFaultMapper.MapToFault(ex);
        }
    }

    public GetSupportedCurrenciesResponse GetSupportedCurrencies()
    {
        try
        {
            _logger.LogInformation("GetSupportedCurrencies called");
            return _currenciesHandler.Handle();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSupportedCurrencies failed");
            throw SoapFaultMapper.MapToFault(ex);
        }
    }

    public GetRateMetadataResponse GetRateMetadata()
    {
        try
        {
            _logger.LogInformation("GetRateMetadata called");
            return _metadataHandler.Handle();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRateMetadata failed");
            throw SoapFaultMapper.MapToFault(ex);
        }
    }
}
