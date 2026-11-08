using CourtBook.Domain.Common;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidAmount_CreatesMoneyInEuro()
    {
        var money = new Money(48m);

        Assert.Equal(48m, money.Amount);
        Assert.Equal("EUR", money.Currency);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Money(-1m));
    }

    [Theory]
    [InlineData("eur", "EUR")]
    [InlineData(" usd ", "USD")]
    public void Constructor_NormalizesCurrency(string input, string expected)
    {
        var money = new Money(10m, input);

        Assert.Equal(expected, money.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    public void Constructor_WithInvalidCurrency_ThrowsDomainException(string currency)
    {
        Assert.Throws<DomainException>(() => new Money(10m, currency));
    }

    [Fact]
    public void Constructor_RoundsToTwoDecimalsAwayFromZero()
    {
        var money = new Money(2.345m);

        Assert.Equal(2.35m, money.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var total = new Money(24m) + new Money(16m);

        Assert.Equal(new Money(40m), total);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Money(24m) + new Money(16m, "USD"));
    }

    [Fact]
    public void TwoMoneyValues_WithSameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(new Money(48m), new Money(48.00m));
    }
}