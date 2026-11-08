namespace CourtBook.Application.Bookings.Pricing;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    public decimal HourlyRate { get; init; } = 24m;
    public decimal PeakHourlyRate { get; init; } = 32m;
    public TimeOnly PeakStartsAt { get; init; } = new(18, 0);
    public TimeOnly PeakEndsAt { get; init; } = new(22, 0);
    public string Currency { get; init; } = "EUR";
}