using System.Net;
using currency_converter.Tests.Integration;

namespace currency_converter.Tests.Contract;

public class WsdlContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WsdlContractTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Wsdl_IsAccessible()
    {
        var response = await _client.GetAsync("/CurrencyConverterService.svc?wsdl");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wsdl = await response.Content.ReadAsStringAsync();
        Assert.Contains("definitions", wsdl);
    }

    [Fact]
    public async Task Wsdl_ContainsAllOperations()
    {
        var response = await _client.GetAsync("/CurrencyConverterService.svc?wsdl");
        var wsdl = await response.Content.ReadAsStringAsync();

        Assert.Contains("ConvertCurrency", wsdl);
        Assert.Contains("GetSupportedCurrencies", wsdl);
        Assert.Contains("GetRateMetadata", wsdl);
    }

    [Fact]
    public async Task Wsdl_ContainsServiceBinding()
    {
        var response = await _client.GetAsync("/CurrencyConverterService.svc?wsdl");
        var wsdl = await response.Content.ReadAsStringAsync();

        Assert.Contains("BasicHttpBinding", wsdl);
        Assert.Contains("ICurrencyConverterService", wsdl);
    }

    [Fact]
    public async Task Wsdl_SubDocument_ContainsDataTypes()
    {
        // CoreWCF splits WSDL into main + imported documents
        var response = await _client.GetAsync("/CurrencyConverterService.svc?wsdl=wsdl0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wsdl = await response.Content.ReadAsStringAsync();

        Assert.Contains("ConvertCurrency", wsdl);
        Assert.Contains("GetSupportedCurrencies", wsdl);
        Assert.Contains("GetRateMetadata", wsdl);
    }
}
