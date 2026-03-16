using System.Net;
using System.Text;

namespace currency_converter.Tests.Integration;

public class SoapEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SoapEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent SoapEnvelope(string body) =>
        new(
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
              <s:Body>
                {body}
              </s:Body>
            </s:Envelope>
            """,
            Encoding.UTF8,
            "text/xml");

    [Fact]
    public async Task ConvertCurrency_ValidRequest_Returns200WithSoapResponse()
    {
        var content = SoapEnvelope("""
            <ConvertCurrency xmlns="http://currencyconverter.local/">
              <request xmlns:m="http://currencyconverter.local/models">
                <m:Amount>100</m:Amount>
                <m:FromCurrency>EUR</m:FromCurrency>
                <m:ToCurrency>USD</m:ToCurrency>
              </request>
            </ConvertCurrency>
            """);

        content.Headers.Add("SOAPAction", "\"http://currencyconverter.local/ICurrencyConverterService/ConvertCurrency\"");

        var response = await _client.PostAsync("/CurrencyConverterService.svc", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ConvertCurrencyResult", body);
        Assert.Contains("OriginalAmount", body);
        Assert.Contains("ConvertedAmount", body);
    }

    [Fact]
    public async Task GetSupportedCurrencies_Returns200()
    {
        var content = SoapEnvelope("""
            <GetSupportedCurrencies xmlns="http://currencyconverter.local/" />
            """);

        content.Headers.Add("SOAPAction", "\"http://currencyconverter.local/ICurrencyConverterService/GetSupportedCurrencies\"");

        var response = await _client.PostAsync("/CurrencyConverterService.svc", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("GetSupportedCurrenciesResult", body);
    }

    [Fact]
    public async Task GetRateMetadata_Returns200()
    {
        var content = SoapEnvelope("""
            <GetRateMetadata xmlns="http://currencyconverter.local/" />
            """);

        content.Headers.Add("SOAPAction", "\"http://currencyconverter.local/ICurrencyConverterService/GetRateMetadata\"");

        var response = await _client.PostAsync("/CurrencyConverterService.svc", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("GetRateMetadataResult", body);
        Assert.Contains("ECB", body);
    }

    [Fact]
    public async Task ConvertCurrency_InvalidCurrency_ReturnsSoapFault()
    {
        var content = SoapEnvelope("""
            <ConvertCurrency xmlns="http://currencyconverter.local/">
              <request xmlns:m="http://currencyconverter.local/models">
                <m:Amount>100</m:Amount>
                <m:FromCurrency>INVALID</m:FromCurrency>
                <m:ToCurrency>USD</m:ToCurrency>
              </request>
            </ConvertCurrency>
            """);

        content.Headers.Add("SOAPAction", "\"http://currencyconverter.local/ICurrencyConverterService/ConvertCurrency\"");

        var response = await _client.PostAsync("/CurrencyConverterService.svc", content);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Fault", body);
    }
}
