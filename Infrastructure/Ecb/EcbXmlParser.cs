using System.Globalization;
using System.Xml.Linq;
using currency_converter.Domain;

namespace currency_converter.Infrastructure.Ecb;

public static class EcbXmlParser
{
    private static readonly XNamespace EcbNs = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

    public static ExchangeRateSnapshot Parse(string xml, DateTime fetchedAtUtc)
    {
        var doc = XDocument.Parse(xml);

        var cubeRoot = doc.Root!
            .Element(EcbNs + "Cube")!
            .Element(EcbNs + "Cube")!;

        var timeAttr = cubeRoot.Attribute("time")?.Value
            ?? throw new FormatException("Missing 'time' attribute on Cube element.");

        var rateDate = DateOnly.Parse(timeAttr, CultureInfo.InvariantCulture);

        var rates = new Dictionary<CurrencyCode, decimal>();

        foreach (var cube in cubeRoot.Elements(EcbNs + "Cube"))
        {
            var currency = cube.Attribute("currency")?.Value;
            var rate = cube.Attribute("rate")?.Value;

            if (currency is null || rate is null)
                continue;

            rates[new CurrencyCode(currency)] = decimal.Parse(rate, CultureInfo.InvariantCulture);
        }

        return new ExchangeRateSnapshot(rateDate, rates, fetchedAtUtc);
    }
}
