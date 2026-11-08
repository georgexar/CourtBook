using CourtBook.Application.Bookings;
using CourtBook.Domain.Entities;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;

namespace CourtBook.UnitTests.Application.Bookings;

public class AvailabilityCalculatorTests
{
    // Πέμπτη 12/11/2026, κέντρο ανοιχτό 08:00–23:00
    private static readonly DateOnly Thursday = new(2026, 11, 12);
    private static readonly DailyOpeningHours OpenEightToEleven =
        new(DayOfWeek.Thursday, new(8, 0), new(23, 0));

    private static DateTimeOffset ThursdayAt(int hour, int minute = 0) =>
        new(2026, 11, 12, hour, minute, 0, TimeSpan.FromHours(2));

    // Υπολογίζει τα slots με «κανονικές» τιμές· τα tests αλλάζουν ό,τι τα ενδιαφέρει
    private static IReadOnlyList<TimeSlotResponse> Calculate(
        int durationMinutes = 90,
        IReadOnlyCollection<Booking>? bookings = null,
        DateTimeOffset? now = null,
        DailyOpeningHours? openingHours = null)
    {
        return AvailabilityCalculator.Calculate(
            openingHours ?? OpenEightToEleven,
            Thursday,
            TimeSpan.FromMinutes(durationMinutes),
            bookings ?? [],
            BookingRuleContextFactory.Athens,
            now ?? DomainFactory.Now);   // Τρίτη 10/11, άρα όλη η Πέμπτη είναι στο μέλλον
    }

    [Fact]
    public void Calculate_WhenCenterIsClosed_ReturnsNoSlots()
    {
        var slots = AvailabilityCalculator.Calculate(
            null, Thursday, TimeSpan.FromMinutes(90), [], BookingRuleContextFactory.Athens, DomainFactory.Now);

        Assert.Empty(slots);
    }

    [Fact]
    public void Calculate_With90Minutes_ReturnsSlotsEvery30MinutesUntilClosing()
    {
        // Act
        var slots = Calculate(durationMinutes: 90);

        // Assert: 08:00, 08:30, ..., 21:30 → 28 slots
        Assert.Equal(28, slots.Count);
        Assert.Equal(ThursdayAt(8), slots[0].Start);
        Assert.Equal(ThursdayAt(21, 30), slots[^1].Start);
        Assert.Equal(ThursdayAt(23), slots[^1].End);
        Assert.All(slots, slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public void Calculate_With60Minutes_LastSlotStartsAt22()
    {
        var slots = Calculate(durationMinutes: 60);

        Assert.Equal(ThursdayAt(22), slots[^1].Start);
    }

    [Fact]
    public void Calculate_MarksSlotsOverlappingAnActiveBookingAsUnavailable()
    {
        // Arrange: κράτηση 19:00–20:30
        var booking = DomainFactory.CreateBooking(startTime: ThursdayAt(19), duration: TimeSpan.FromMinutes(90));

        // Act
        var slots = Calculate(durationMinutes: 90, bookings: [booking]);

        // Assert: τα slots που ξεκινούν 18:00 έως 20:00 «πέφτουν» πάνω της
        var unavailableStarts = slots.Where(s => !s.IsAvailable).Select(s => s.Start).ToList();

        Assert.Equal(
            [ThursdayAt(18), ThursdayAt(18, 30), ThursdayAt(19), ThursdayAt(19, 30), ThursdayAt(20)],
            unavailableStarts);
    }

    [Fact]
    public void Calculate_IgnoresCancelledBookings()
    {
        // Arrange
        var booking = DomainFactory.CreateBooking(startTime: ThursdayAt(19));
        booking.Cancel(DomainFactory.Now);

        // Act
        var slots = Calculate(bookings: [booking]);

        // Assert
        Assert.All(slots, slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public void Calculate_MarksPastSlotsAsUnavailable()
    {
        // Arrange: «τώρα» είναι Πέμπτη 12:00
        var now = ThursdayAt(12);

        // Act
        var slots = Calculate(now: now);

        // Assert
        Assert.False(slots.Single(s => s.Start == ThursdayAt(11, 30)).IsAvailable);
        Assert.False(slots.Single(s => s.Start == ThursdayAt(12)).IsAvailable);   // ξεκινά ΤΩΡΑ, άρα όχι
        Assert.True(slots.Single(s => s.Start == ThursdayAt(12, 30)).IsAvailable);
    }
}