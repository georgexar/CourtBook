using CourtBook.Application.Bookings;
using CourtBook.Application.Common;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;
using CourtBook.Infrastructure.Persistence;
using CourtBook.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtBook.IntegrationTests.Bookings;

public sealed class BookingConcurrencyTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private const int SimultaneousUsers = 10;

    [Fact]
    public async Task CreateAsync_ManySimultaneousRequestsForSameSlot_OnlyOneBookingIsSaved()
    {
        // Arrange: ένα γήπεδο, και ΕΝΑ slot που θέλουν όλοι
        var courtId = await CreateCourtAsync();
        var start = NextWeekAt(hour: 19);
        var request = new CreateBookingRequest(courtId, start, start.AddMinutes(90));

        var startGate = new TaskCompletionSource();

        // 10 διαφορετικοί χρήστες ετοιμάζονται και περιμένουν στην πύλη
        var attempts = Enumerable.Range(0, SimultaneousUsers)
            .Select(_ => TryCreateBookingAsync(request, Guid.CreateVersion7(), startGate.Task))
            .ToList();

        // Act: η πύλη ανοίγει, όλοι ξεκινάνε μαζί
        startGate.SetResult();
        var results = await Task.WhenAll(attempts);

        // Assert
        var successful = results.Count(succeeded => succeeded);
        var savedBookings = await CountActiveBookingsAsync(courtId);

        Assert.Equal(1, savedBookings);
        Assert.Equal(1, successful);
    }

    // ========== Βοηθητικές μέθοδοι ==========

    // Ένας «χρήστης» κάνει κράτηση, σε δικό του scope (σαν ξεχωριστό HTTP request)
    private async Task<bool> TryCreateBookingAsync(CreateBookingRequest request, Guid userId, Task startGate)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<FakeCurrentUser>().UserId = userId;
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await startGate;   // ⏸ περιμένει το «πιστόλι»

        try
        {
            await bookingService.CreateAsync(request);
            return true;
        }
        catch (Exception ex) when (ex is BookingRuleViolationException or ConflictException)
        {
            return false;
        }
    }

    // Ένα κέντρο ανοιχτό κάθε μέρα 08:00–23:00, με ένα γήπεδο
    private async Task<Guid> CreateCourtAsync()
    {
        var center = new SportsCenter($"Concurrency Club {Guid.NewGuid():N}", "Test Street 1", "Piraeus");
        center.SetOpeningHours(
            Enum.GetValues<DayOfWeek>()
                .Select(day => new DailyOpeningHours(day, new(8, 0), new(23, 0))));

        var court = center.AddCourt("Padel 1", SportType.Padel);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SportsCenters.Add(center);
        await db.SaveChangesAsync();

        return court.Id;
    }

    private async Task<int> CountActiveBookingsAsync(Guid courtId)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Bookings.CountAsync(b =>
            b.CourtId == courtId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
    }

    // Η ίδια ώρα (ώρα Ελλάδας), μία εβδομάδα από σήμερα
    private static DateTimeOffset NextWeekAt(int hour)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var local = date.ToDateTime(new TimeOnly(hour, 0));
        return new DateTimeOffset(local, AppTimeZone.Default.GetUtcOffset(local));
    }
}