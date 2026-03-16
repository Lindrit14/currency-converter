using System.Text.RegularExpressions;

namespace currency_converter.Domain;

public readonly partial record struct CurrencyCode
{
    public string Value { get; }

    public CurrencyCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var upper = value.Trim().ToUpperInvariant();

        if (!Iso4217Regex().IsMatch(upper))
            throw new ArgumentException($"Invalid currency code: '{value}'. Must be a 3-letter ISO 4217 code.", nameof(value));

        Value = upper;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Iso4217Regex();
}
