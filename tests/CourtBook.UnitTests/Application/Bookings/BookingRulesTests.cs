using CourtBook.Application.Bookings.Rules;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;

namespace CourtBook.UnitTests.Application.Bookings;

public class BookingRulesTests
{
    // ---------- Το «κανονικό» context περνάει όλους τους κανόνες ----------

    [Fact]
    public void ValidContext_PassesAllRules()
    {
        // Arrange
        var context = BookingRuleContextFactory.Valid();
        IBookingRule[] rules =
        [
            new CourtMustBeActiveRule(),
            new AllowedDurationRule(),
            new WithinOpeningHoursRule(),
            new NoOverlapRule(),
            new MaxActiveBookingsPerUserRule()
        ];

        // Act & Assert
        Assert.All(rules, rule => Assert.Null(rule.Validate(context)));
    }

    // ---------- CourtMustBeActiveRule ----------

    [Fact]
    public void CourtMustBeActive_WhenCourtIsInactive_ReturnsError()
    {
        // Arrange
        var context = BookingRuleContextFactory.Valid();
        context.Court.Deactivate();

        // Act
        var error = new CourtMustBeActiveRule().Validate(context);

        // Assert
        Assert.NotNull(error);
    }

    // ---------- AllowedDurationRule ----------

    [Theory]
    [InlineData(60, true)]
    [InlineData(90, true)]
    [InlineData(30, false)]
    [InlineData(75, false)]
    [InlineData(120, false)]
    public void AllowedDuration_ReturnsExpectedResult(int minutes, bool isValid)
    {
        // Arrange
        var context = BookingRuleContextFactory.At(19, 0, minutes);

        // Act
        var error = new AllowedDurationRule().Validate(context);

        // Assert
        Assert.Equal(isValid, error is null);
    }

    // ---------- WithinOpeningHoursRule (ωράριο 08:00–23:00) ----------

    [Theory]
    [InlineData(8, 0, 60, true)]     // ξεκινά ακριβώς με το άνοιγμα
    [InlineData(21, 30, 90, true)]   // τελειώνει ακριβώς με το κλείσιμο
    [InlineData(7, 30, 60, false)]   // ξεκινά πριν ανοίξει
    [InlineData(22, 30, 60, false)]  // τελειώνει αφού κλείσει
    [InlineData(23, 30, 60, false)]  // περνάει τα μεσάνυχτα
    public void WithinOpeningHours_ReturnsExpectedResult(int hour, int minute, int durationMinutes, bool isValid)
    {
        // Arrange
        var context = BookingRuleContextFactory.At(hour, minute, durationMinutes);

        // Act
        var error = new WithinOpeningHoursRule().Validate(context);

        // Assert
        Assert.Equal(isValid, error is null);
    }

    [Fact]
    public void WithinOpeningHours_WhenCenterIsClosedThatDay_ReturnsError()
    {
        // Arrange: το κέντρο είναι ανοιχτό μόνο Δευτέρα, και η κράτηση είναι Πέμπτη
        var context = BookingRuleContextFactory.Valid();
        context.Center.SetOpeningHours([new DailyOpeningHours(DayOfWeek.Monday, new(8, 0), new(23, 0))]);

        // Act
        var error = new WithinOpeningHoursRule().Validate(context);

        // Assert
        Assert.NotNull(error);
        Assert.Contains("closed", error);
    }

    [Fact]
    public void WithinOpeningHours_ConvertsUtcToLocalTime()
    {
        // Arrange: 17:00 UTC = 19:00 ώρα Ελλάδας (χειμώνας, +02:00), άρα μέσα στο ωράριο
        var utcStart = new DateTimeOffset(2026, 11, 12, 17, 0, 0, TimeSpan.Zero);
        var context = BookingRuleContextFactory.Valid() with { Start = utcStart, End = utcStart.AddMinutes(90) };

        // Act
        var error = new WithinOpeningHoursRule().Validate(context);

        // Assert
        Assert.Null(error);
        Assert.Equal(19, context.LocalStart.Hour);
    }

    // ---------- NoOverlapRule (νέα κράτηση: Πέμπτη 19:00–20:30) ----------

    [Fact]
    public void NoOverlap_WhenOverlappingActiveBookingExists_ReturnsError()
    {
        // Arrange: υπάρχει ήδη κράτηση 19:30–21:00
        var existing = DomainFactory.CreateBooking(
            startTime: BookingRuleContextFactory.DefaultStart.AddMinutes(30),
            duration: TimeSpan.FromMinutes(90));

        var context = BookingRuleContextFactory.Valid() with { ExistingBookingsOnCourt = [existing] };

        // Act
        var error = new NoOverlapRule().Validate(context);

        // Assert
        Assert.NotNull(error);
    }

    [Fact]
    public void NoOverlap_WhenOverlappingBookingIsCancelled_ReturnsNull()
    {
        // Arrange: η υπάρχουσα κράτηση ακυρώθηκε, άρα η ώρα είναι ελεύθερη
        var existing = DomainFactory.CreateBooking(startTime: BookingRuleContextFactory.DefaultStart);
        existing.Cancel(DomainFactory.Now);

        var context = BookingRuleContextFactory.Valid() with { ExistingBookingsOnCourt = [existing] };

        // Act
        var error = new NoOverlapRule().Validate(context);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void NoOverlap_WhenExistingBookingEndsExactlyAtStart_ReturnsNull()
    {
        // Arrange: υπάρχουσα κράτηση 17:30–19:00, η νέα ξεκινά 19:00
        var existing = DomainFactory.CreateBooking(
            startTime: BookingRuleContextFactory.DefaultStart.AddMinutes(-90),
            duration: TimeSpan.FromMinutes(90));

        var context = BookingRuleContextFactory.Valid() with { ExistingBookingsOnCourt = [existing] };

        // Act
        var error = new NoOverlapRule().Validate(context);

        // Assert
        Assert.Null(error);
    }

    // ---------- MaxActiveBookingsPerUserRule ----------

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    public void MaxActiveBookings_ReturnsExpectedResult(int activeBookings, bool isValid)
    {
        // Arrange
        var context = BookingRuleContextFactory.Valid() with { UserActiveBookingsCount = activeBookings };

        // Act
        var error = new MaxActiveBookingsPerUserRule().Validate(context);

        // Assert
        Assert.Equal(isValid, error is null);
    }
}