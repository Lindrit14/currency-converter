using currency_converter.Domain;

namespace currency_converter.Tests.Domain;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData(" gbp ")]
    public void Constructor_ValidCode_NormalizesToUpperCase(string input)
    {
        var code = new CurrencyCode(input);
        Assert.Equal(input.Trim().ToUpperInvariant(), code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    [InlineData("U$D")]
    public void Constructor_InvalidCode_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => new CurrencyCode(input));
    }

    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CurrencyCode(null!));
    }

    [Fact]
    public void Equality_SameCode_AreEqual()
    {
        var a = new CurrencyCode("USD");
        var b = new CurrencyCode("usd");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var code = new CurrencyCode("JPY");
        Assert.Equal("JPY", code.ToString());
    }
}
