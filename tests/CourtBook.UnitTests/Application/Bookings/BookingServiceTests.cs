using CourtBook.Application.Bookings;
using CourtBook.Application.Bookings.Rules;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using CourtBook.Application.Bookings.Pricing;
using Microsoft.Extensions.Options;

namespace CourtBook.UnitTests.Application.Bookings;

public sealed class BookingServiceTests : IDisposable
{
    // «Τώρα» = Τρίτη 10/11/2026 12:00. Οι κρατήσεις των tests είναι Πέμπτη 12/11.
    private readonly FakeTimeProvider _timeProvider = new(DomainFactory.Now);
    private readonly SqliteTestDatabase _database;
    private readonly Guid _userId = Guid.CreateVersion7();

    // Οι ΠΡΑΓΜΑΤΙΚΟΙ κανόνες, όπως στην εφαρμογή
    private static readonly IBookingRule[] AllRules =
    [
        new CourtMustBeActiveRule(),
        new AllowedDurationRule(),
        new WithinOpeningHoursRule(),
        new NoOverlapRule(),
        new MaxActiveBookingsPerUserRule()
    ];

    // Ο πραγματικός υπολογιστής, με τις default τιμές (24 €/ώρα, 32 €/ώρα 18:00–22:00)
    private static readonly BookingPriceCalculator PriceCalculator =
        new(Options.Create(new PricingOptions()));

    public BookingServiceTests()
    {
        _database = new SqliteTestDatabase(_timeProvider);
    }

    public void Dispose() => _database.Dispose();

    // ========== Βοηθητικές μέθοδοι ==========

    // Ένα κέντρο ανοιχτό κάθε μέρα 08:00–23:00, με ένα γήπεδο
    private static (SportsCenter Center, Court Court) CenterWithCourt()
    {
        var center = DomainFactory.CreateSportsCenter();
        center.SetOpeningHours(
            Enum.GetValues<DayOfWeek>()
                .Select(day => new DailyOpeningHours(day, new(8, 0), new(23, 0))));

        var court = center.AddCourt("Padel 1", SportType.Padel);
        return (center, court);
    }

    // Πέμπτη 12/11/2026 στην ώρα που δίνουμε (ώρα Ελλάδας)
    private static DateTimeOffset Thursday(int hour, int minute = 0) =>
        new(2026, 11, 12, hour, minute, 0, TimeSpan.FromHours(2));

    // Ένα request κράτησης
    private CreateBookingRequest Request(Court court, int hour, int minute = 0, int minutes = 90, Guid? userId = null) =>
        new(court.Id, userId ?? _userId, Thursday(hour, minute), Thursday(hour, minute).AddMinutes(minutes));

    // Μια ήδη επιβεβαιωμένη κράτηση (για να υπάρχει από πριν στη βάση)
    private Booking ExistingBooking(Court court, int hour, int minutes = 90, Guid? userId = null)
    {
        var booking = new Booking(court.Id, userId ?? _userId, Thursday(hour), Thursday(hour).AddMinutes(minutes), new Money(48m), DomainFactory.Now);
        booking.Confirm();
        return booking;
    }

    // Arrange: αποθηκεύει ό,τι του δώσουμε (κέντρα, κρατήσεις)
    private async Task SaveAsync(params object[] entities)
    {
        await using var context = _database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    // Act: καλεί το service με δικό του context (σαν ένα HTTP request)
    private async Task<Guid> CreateBookingAsync(CreateBookingRequest request)
    {
        await using var context = _database.CreateContext();
        var service = new BookingService(context, AllRules, PriceCalculator, _timeProvider);
        
        return await service.CreateAsync(request);
    }

    // Assert: πόσες κρατήσεις υπάρχουν στη βάση
    private async Task<int> CountBookingsAsync()
    {
        await using var context = _database.CreateContext();
        return await context.Bookings.CountAsync();
    }

    // ========== Tests ==========

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesConfirmedBooking()
    {
        // Arrange
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        // Act
        var id = await CreateBookingAsync(Request(court, hour: 19));

        // Assert
        await using var context = _database.CreateContext();
        var saved = await context.Bookings.SingleAsync(b => b.Id == id);

        Assert.Equal(court.Id, saved.CourtId);
        Assert.Equal(_userId, saved.UserId);
        Assert.Equal(Thursday(19), saved.StartTime);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        Assert.Equal(DomainFactory.Now, saved.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_WhenCourtDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange: ένα γήπεδο που ΔΕΝ αποθηκεύτηκε ποτέ
        var (_, court) = CenterWithCourt();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => CreateBookingAsync(Request(court, hour: 19)));
    }

    [Fact]
    public async Task CreateAsync_WhenOverlappingBookingExistsInDatabase_ThrowsAndSavesNothing()
    {
        // Arrange: στη βάση υπάρχει ήδη κράτηση 19:00–20:30
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);
        await SaveAsync(ExistingBooking(court, hour: 19, userId: Guid.CreateVersion7()));

        // Act: ζητάμε 19:30–21:00
        var exception = await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            CreateBookingAsync(Request(court, hour: 19, minute: 30)));

        // Assert
        Assert.Contains(exception.Errors, error => error.Contains("already booked"));
        Assert.Equal(1, await CountBookingsAsync());   // μόνο η παλιά
    }

    [Fact]
    public async Task CreateAsync_IgnoresCancelledBookingsWhenCheckingOverlap()
    {
        // Arrange: στη βάση υπάρχει ακυρωμένη κράτηση 19:00–20:30
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        var cancelled = ExistingBooking(court, hour: 19, userId: Guid.CreateVersion7());
        cancelled.Cancel(DomainFactory.Now);
        await SaveAsync(cancelled);

        // Act: η ίδια ώρα είναι ελεύθερη
        await CreateBookingAsync(Request(court, hour: 19));

        // Assert
        Assert.Equal(2, await CountBookingsAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenUserHasThreeActiveBookings_ThrowsRuleViolation()
    {
        // Arrange: ο χρήστης έχει ήδη 3 ενεργές κρατήσεις
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);
        await SaveAsync(
            ExistingBooking(court, hour: 8),
            ExistingBooking(court, hour: 10),
            ExistingBooking(court, hour: 12));

        // Act
        var exception = await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            CreateBookingAsync(Request(court, hour: 19)));

        // Assert
        Assert.Contains(exception.Errors, error => error.Contains("active bookings"));
    }

    [Fact]
    public async Task CreateAsync_DoesNotCountCancelledBookingsTowardsUserLimit()
    {
        // Arrange: 3 κρατήσεις, αλλά η μία είναι ακυρωμένη → ενεργές 2
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        var cancelled = ExistingBooking(court, hour: 12);
        cancelled.Cancel(DomainFactory.Now);

        await SaveAsync(
            ExistingBooking(court, hour: 8),
            ExistingBooking(court, hour: 10),
            cancelled);

        // Act: η 4η επιτρέπεται, γιατί οι ΕΝΕΡΓΕΣ είναι 2
        await CreateBookingAsync(Request(court, hour: 19));

        // Assert
        Assert.Equal(4, await CountBookingsAsync());
    }

    [Fact]
    public async Task CreateAsync_WithMultipleViolations_ReturnsAllErrors()
    {
        // Arrange
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        // Act: 22:45–23:15 → 30 λεπτά ΚΑΙ εκτός ωραρίου
        var exception = await Assert.ThrowsAsync<BookingRuleViolationException>(() =>
            CreateBookingAsync(Request(court, hour: 22, minute: 45, minutes: 30)));

        // Assert: δύο σφάλματα, όχι μόνο το πρώτο
        Assert.Equal(2, exception.Errors.Count);
    }

    [Theory]
    [InlineData(19, 48.00)]   // όλο σε αιχμή
    [InlineData(17, 40.00)]   // μπαίνει στην αιχμή στις 18:00
    [InlineData(10, 36.00)]   // εκτός αιχμής
    public async Task CreateAsync_SavesCalculatedPrice(int hour, double expectedPrice)
    {
        // Arrange
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        // Act: κράτηση 90 λεπτών
        var id = await CreateBookingAsync(Request(court, hour: hour));

        // Assert: η τιμή αποθηκεύτηκε στη βάση
        await using var context = _database.CreateContext();
        var saved = await context.Bookings.SingleAsync(b => b.Id == id);

        Assert.Equal(new Money((decimal)expectedPrice), saved.Price);
    }
}