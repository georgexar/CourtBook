using CourtBook.Application.Bookings.Pricing;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;
using Microsoft.Extensions.Options;

namespace CourtBook.UnitTests.Application.Bookings;

public class BookingPriceCalculatorTests
{
    // Οι default ρυθμίσεις: 24 €/ώρα, 32 €/ώρα στις 18:00–22:00
    private readonly BookingPriceCalculator _calculator = new(Options.Create(new PricingOptions()));

    private static DateTimeOffset ThursdayAt(int hour, int minute = 0) =>
        new(2026, 11, 12, hour, minute, 0, TimeSpan.FromHours(2));

    [Theory]
    [InlineData(10, 0, 60, 24.00)]    // εκτός αιχμής: 2 × 12
    [InlineData(10, 0, 90, 36.00)]    // εκτός αιχμής: 3 × 12
    [InlineData(19, 0, 90, 48.00)]    // όλο σε αιχμή: 3 × 16
    [InlineData(17, 0, 90, 40.00)]    // ξεκινά εκτός, τελειώνει μέσα: 12 + 12 + 16
    [InlineData(21, 30, 60, 28.00)]   // ξεκινά μέσα, τελειώνει εκτός: 16 + 12
    public void Calculate_ReturnsExpectedPrice(int hour, int minute, int durationMinutes, double expected)
    {
        // Arrange
        var start = ThursdayAt(hour, minute);
        var end = start.AddMinutes(durationMinutes);

        // Act
        var price = _calculator.Calculate(start, end, BookingRuleContextFactory.Athens);

        // Assert
        Assert.Equal(new Money((decimal)expected), price);
    }

    [Fact]
    public void Calculate_ConvertsUtcToLocalTimeBeforeApplyingPeakHours()
    {
        // Arrange: 17:00 UTC = 19:00 ώρα Ελλάδας, άρα αιχμή
        var start = new DateTimeOffset(2026, 11, 12, 17, 0, 0, TimeSpan.Zero);

        // Act
        var price = _calculator.Calculate(start, start.AddMinutes(90), BookingRuleContextFactory.Athens);

        // Assert
        Assert.Equal(new Money(48m), price);
    }

    [Fact]
    public void Calculate_UsesConfiguredRates()
    {
        // Arrange: άλλες τιμές, σαν να άλλαξαν στο appsettings.json
        var calculator = new BookingPriceCalculator(Options.Create(new PricingOptions
        {
            HourlyRate = 20m,
            PeakHourlyRate = 30m
        }));

        // Act: 19:00–20:00, όλο σε αιχμή
        var price = calculator.Calculate(ThursdayAt(19), ThursdayAt(20), BookingRuleContextFactory.Athens);

        // Assert
        Assert.Equal(new Money(30m), price);
    }
}