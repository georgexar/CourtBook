using CourtBook.Application.Bookings;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;
using CourtBook.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CourtBook.UnitTests.Application.Bookings;

public sealed class BookingMaintenanceServiceTests : IDisposable
{
    // «Τώρα» = Τρίτη 10/11/2026 12:00. Οι κρατήσεις είναι Πέμπτη 12/11.
    private readonly FakeTimeProvider _timeProvider = new(DomainFactory.Now);
    private readonly SqliteTestDatabase _database;

    public BookingMaintenanceServiceTests()
    {
        _database = new SqliteTestDatabase(_timeProvider);
    }

    public void Dispose() => _database.Dispose();

    // ========== Tests ==========

    [Fact]
    public async Task CompleteFinishedBookings_WhenBookingHasEnded_MarksItCompleted()
    {
        var court = await SaveCourtAsync();
        var booking = await SaveBookingAsync(court, hour: 19);     // 19:00–20:30
        _timeProvider.SetUtcNow(Thursday(21));                      // τώρα: 21:00

        var completed = await RunAsync();

        Assert.Equal(1, completed);
        Assert.Equal(BookingStatus.Completed, await StatusOfAsync(booking.Id));
    }

    [Fact]
    public async Task CompleteFinishedBookings_WhenBookingIsInProgress_LeavesItConfirmed()
    {
        var court = await SaveCourtAsync();
        var booking = await SaveBookingAsync(court, hour: 19);
        _timeProvider.SetUtcNow(Thursday(20));                      // παίζουν ακόμα

        var completed = await RunAsync();

        Assert.Equal(0, completed);
        Assert.Equal(BookingStatus.Confirmed, await StatusOfAsync(booking.Id));
    }

    [Fact]
    public async Task CompleteFinishedBookings_WhenBookingWasCancelled_LeavesItCancelled()
    {
        var court = await SaveCourtAsync();
        var booking = await SaveBookingAsync(court, hour: 19, cancelled: true);
        _timeProvider.SetUtcNow(Thursday(21));

        var completed = await RunAsync();

        Assert.Equal(0, completed);
        Assert.Equal(BookingStatus.Cancelled, await StatusOfAsync(booking.Id));
    }

    // ========== Βοηθητικές μέθοδοι ==========

    private static DateTimeOffset Thursday(int hour) =>
        new(2026, 11, 12, hour, 0, 0, TimeSpan.FromHours(2));

    private async Task<Court> SaveCourtAsync()
    {
        var center = DomainFactory.CreateSportsCenter();
        var court = center.AddCourt("Padel 1", SportType.Padel);

        await using var db = _database.CreateContext();
        db.SportsCenters.Add(center);
        await db.SaveChangesAsync();

        return court;
    }

    private async Task<Booking> SaveBookingAsync(Court court, int hour, bool cancelled = false)
    {
        var now = _timeProvider.GetUtcNow();
        var booking = new Booking(court.Id, Guid.CreateVersion7(), Thursday(hour), Thursday(hour).AddMinutes(90), new Money(48m), now);
        booking.Confirm();

        if (cancelled)
            booking.Cancel(now);

        await using var db = _database.CreateContext();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return booking;
    }

    private async Task<int> RunAsync()
    {
        await using var db = _database.CreateContext();
        var service = new BookingMaintenanceService(db, _timeProvider);

        return await service.CompleteFinishedBookingsAsync();
    }

    private async Task<BookingStatus> StatusOfAsync(Guid bookingId)
    {
        await using var db = _database.CreateContext();

        return await db.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => b.Status)
            .SingleAsync();
    }
}