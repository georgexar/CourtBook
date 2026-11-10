using CourtBook.Application.Bookings;
using CourtBook.Application.Bookings.Pricing;
using CourtBook.Application.Bookings.Rules;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.Common.Security;  
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CourtBook.UnitTests.Application.Bookings;

public sealed class BookingServiceTests : IDisposable
{
    // «Τώρα» = Τρίτη 10/11/2026 12:00. Οι κρατήσεις των tests είναι Πέμπτη 12/11.
    private readonly FakeTimeProvider _timeProvider = new(DomainFactory.Now);
    private readonly SqliteTestDatabase _database;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly FakeCurrentUser _currentUser;  

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
        _currentUser = new FakeCurrentUser { UserId = _userId };  
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

    // Ένα request κράτησης (χωρίς userId: ο χρήστης έρχεται από το _currentUser)  
    private static CreateBookingRequest Request(Court court, int hour, int minute = 0, int minutes = 90) =>
        new(court.Id, Thursday(hour, minute), Thursday(hour, minute).AddMinutes(minutes));

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

    // Φτιάχνει το service με όλες τις εξαρτήσεις του (σε ΕΝΑ σημείο)  
    private BookingService CreateService(CourtBook.Infrastructure.Persistence.AppDbContext context) =>
        new(context, _currentUser, AllRules, PriceCalculator, _timeProvider);

    // Act: καλεί το service με δικό του context (σαν ένα HTTP request)
    private async Task<Guid> CreateBookingAsync(CreateBookingRequest request)
    {
        await using var context = _database.CreateContext();
        return await CreateService(context).CreateAsync(request);
    }

    // Act: τρέχει κάτι στο service, με δικό του context
    private async Task RunServiceAsync(Func<BookingService, Task> action)
    {
        await using var context = _database.CreateContext();
        await action(CreateService(context));
    }

    private async Task<T> RunServiceAsync<T>(Func<BookingService, Task<T>> action)
    {
        await using var context = _database.CreateContext();
        return await action(CreateService(context));
    }

    // Assert: πόσες κρατήσεις υπάρχουν στη βάση
    private async Task<int> CountBookingsAsync()
    {
        await using var context = _database.CreateContext();
        return await context.Bookings.CountAsync();
    }

    // Assert: διαβάζει μια κράτηση φρέσκια από τη βάση
    private async Task<Booking> FindBookingAsync(Guid bookingId)
    {
        await using var context = _database.CreateContext();
        return await context.Bookings.SingleAsync(b => b.Id == bookingId);
    }

    // ========== CreateAsync ==========

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesConfirmedBookingForCurrentUser()
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
        Assert.Equal(_userId, saved.UserId);   // ο χρήστης ήρθε από το _currentUser
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
        // Arrange: στη βάση υπάρχει ήδη κράτηση 19:00–20:30 (άλλου χρήστη)
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
        // Arrange: ο τρέχων χρήστης έχει ήδη 3 ενεργές κρατήσεις
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

    // ========== CancelAsync ==========

    [Fact]
    public async Task CancelAsync_ByOwnerMoreThan24HoursBefore_CancelsWithoutLateFlag()
    {
        // Arrange: κράτηση Πέμπτη 19:00, «τώρα» Τρίτη 12:00 (55 ώρες πριν)
        var (center, court) = CenterWithCourt();
        var booking = ExistingBooking(court, hour: 19);
        await SaveAsync(center);
        await SaveAsync(booking);

        // Act   ← ΑΛΛΑΓΗ ④: χωρίς userId
        await RunServiceAsync(service => service.CancelAsync(booking.Id));

        // Assert
        var saved = await FindBookingAsync(booking.Id);
        Assert.Equal(BookingStatus.Cancelled, saved.Status);
        Assert.False(saved.IsLateCancellation);
    }

    [Fact]
    public async Task CancelAsync_LessThan24HoursBefore_MarksLateCancellation()
    {
        // Arrange
        var (center, court) = CenterWithCourt();
        var booking = ExistingBooking(court, hour: 19);
        await SaveAsync(center);
        await SaveAsync(booking);

        _timeProvider.Advance(TimeSpan.FromHours(46));   // τώρα: Πέμπτη 10:00, 9 ώρες πριν

        // Act   ← ΑΛΛΑΓΗ ④
        await RunServiceAsync(service => service.CancelAsync(booking.Id));

        // Assert
        var saved = await FindBookingAsync(booking.Id);
        Assert.True(saved.IsLateCancellation);
    }

    [Fact]
    public async Task CancelAsync_ByAnotherUser_ThrowsNotFoundAndKeepsBooking()
    {
        // Arrange: η κράτηση ανήκει στον _userId
        var (center, court) = CenterWithCourt();
        var booking = ExistingBooking(court, hour: 19);
        await SaveAsync(center);
        await SaveAsync(booking);

        // ← ΑΛΛΑΓΗ ⑤: τώρα είναι συνδεδεμένος ΑΛΛΟΣ χρήστης
        _currentUser.UserId = Guid.CreateVersion7();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            RunServiceAsync(service => service.CancelAsync(booking.Id)));

        var saved = await FindBookingAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
    }

    // ========== GetMyBookingsAsync ==========  

    [Fact]
    public async Task GetMyBookingsAsync_ReturnsOnlyCurrentUsersBookingsNewestFirstInPages()
    {
        // Arrange: 3 κρατήσεις του τρέχοντος χρήστη, 1 άλλου χρήστη
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);
        await SaveAsync(
            ExistingBooking(court, hour: 8),
            ExistingBooking(court, hour: 10),
            ExistingBooking(court, hour: 12),
            ExistingBooking(court, hour: 14, userId: Guid.CreateVersion7()));

        // Act
        var firstPage = await RunServiceAsync(service => service.GetMyBookingsAsync(page: 1, pageSize: 2));
        var secondPage = await RunServiceAsync(service => service.GetMyBookingsAsync(page: 2, pageSize: 2));

        // Assert: σελίδα 1 → 12:00, 10:00
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.True(firstPage.HasNextPage);
        Assert.Equal([Thursday(12), Thursday(10)], firstPage.Items.Select(b => b.Start));

        // σελίδα 2 → 08:00
        var last = Assert.Single(secondPage.Items);
        Assert.Equal(Thursday(8), last.Start);
        Assert.False(secondPage.HasNextPage);
    }

    [Fact]
    public async Task GetMyBookingsAsync_WithStatusFilter_ReturnsOnlyMatchingBookings()
    {
        // Arrange: 2 confirmed, 1 cancelled
        var (center, court) = CenterWithCourt();
        await SaveAsync(center);

        var cancelled = ExistingBooking(court, hour: 12);
        cancelled.Cancel(DomainFactory.Now);

        await SaveAsync(
            ExistingBooking(court, hour: 8),
            ExistingBooking(court, hour: 10),
            cancelled);

        // Act
        var result = await RunServiceAsync(service =>
            service.GetMyBookingsAsync(page: 1, pageSize: 10, status: BookingStatus.Cancelled));

        // Assert
        var only = Assert.Single(result.Items);
        Assert.Equal(BookingStatus.Cancelled, only.Status);
    }

    // ========== GetByIdAsync: ποιος βλέπει τι ==========   ← ΝΕΑ TESTS

    [Fact]
    public async Task GetByIdAsync_ForAnotherUsersBooking_ReturnsNull()
    {
        // Arrange: κράτηση άλλου χρήστη
        var (center, court) = CenterWithCourt();
        var othersBooking = ExistingBooking(court, hour: 19, userId: Guid.CreateVersion7());
        await SaveAsync(center);
        await SaveAsync(othersBooking);

        // Act: ο τρέχων χρήστης (Player) ζητάει την κράτηση του άλλου
        var result = await RunServiceAsync(service => service.GetByIdAsync(othersBooking.Id));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AsManager_CanSeeAnyBooking()
    {
        // Arrange
        var (center, court) = CenterWithCourt();
        var othersBooking = ExistingBooking(court, hour: 19, userId: Guid.CreateVersion7());
        await SaveAsync(center);
        await SaveAsync(othersBooking);

        _currentUser.Roles.Add(Roles.Manager);

        // Act
        var result = await RunServiceAsync(service => service.GetByIdAsync(othersBooking.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(othersBooking.Id, result.Id);
    }
}