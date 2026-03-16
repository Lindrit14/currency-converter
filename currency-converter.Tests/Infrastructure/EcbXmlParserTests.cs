using currency_converter.Domain;
using currency_converter.Infrastructure.Ecb;

namespace currency_converter.Tests.Infrastructure;

public class EcbXmlParserTests
{
    private const string SampleXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                         xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
            <gesmes:subject>Reference rates</gesmes:subject>
            <gesmes:Sender>
                <gesmes:name>European Central Bank</gesmes:name>
            </gesmes:Sender>
            <Cube>
                <Cube time="2024-12-20">
                    <Cube currency="USD" rate="1.0440"/>
                    <Cube currency="JPY" rate="157.02"/>
                    <Cube currency="GBP" rate="0.83098"/>
                </Cube>
            </Cube>
        </gesmes:Envelope>
        """;

    [Fact]
    public void Parse_ValidXml_ReturnsSnapshot()
    {
        var fetchedAt = new DateTime(2024, 12, 20, 16, 15, 0, DateTimeKind.Utc);
        var snapshot = EcbXmlParser.Parse(SampleXml, fetchedAt);

        Assert.Equal(new DateOnly(2024, 12, 20), snapshot.RateDate);
        Assert.Equal(fetchedAt, snapshot.LastRefreshUtc);
        Assert.Equal(1.0440m, snapshot.GetRate(new CurrencyCode("USD")));
        Assert.Equal(157.02m, snapshot.GetRate(new CurrencyCode("JPY")));
        Assert.Equal(0.83098m, snapshot.GetRate(new CurrencyCode("GBP")));
    }

    [Fact]
    public void Parse_ValidXml_EurReturnsOne()
    {
        var snapshot = EcbXmlParser.Parse(SampleXml, DateTime.UtcNow);
        Assert.Equal(1m, snapshot.GetRate(new CurrencyCode("EUR")));
    }

    [Fact]
    public void Parse_ValidXml_SupportsAllCurrencies()
    {
        var snapshot = EcbXmlParser.Parse(SampleXml, DateTime.UtcNow);
        var currencies = snapshot.GetSupportedCurrencies().Select(c => c.Value).ToList();

        Assert.Contains("EUR", currencies);
        Assert.Contains("USD", currencies);
        Assert.Contains("JPY", currencies);
        Assert.Contains("GBP", currencies);
        Assert.Equal(4, currencies.Count);
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => EcbXmlParser.Parse("not xml", DateTime.UtcNow));
    }
}
