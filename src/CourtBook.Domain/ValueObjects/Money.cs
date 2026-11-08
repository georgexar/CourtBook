using CourtBook.Domain.Common;

namespace CourtBook.Domain.ValueObjects;

public sealed record Money
{
    public const string DefaultCurrency = "EUR";

    public decimal Amount { get; private init; }
    public string Currency { get; private init; } = null!;

    // Για το EF Core
    private Money() { }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");

        currency = Guard.Required(currency, 3, nameof(Currency)).ToUpperInvariant();

        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code (e.g. EUR).");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException($"Cannot add {left.Currency} and {right.Currency}.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}