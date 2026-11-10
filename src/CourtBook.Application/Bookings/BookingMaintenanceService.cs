using CourtBook.Application.Common.Interfaces;
using CourtBook.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Application.Bookings;

internal sealed class BookingMaintenanceService(IAppDbContext db, TimeProvider timeProvider)
    : IBookingMaintenanceService
{
    internal const int BatchSize = 500;

    public async Task<int> CompleteFinishedBookingsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var finishedBookings = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.EndTime <= now)
            .OrderBy(b => b.EndTime)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var booking in finishedBookings)
        {
            booking.Complete(now);
        }

        await db.SaveChangesAsync(cancellationToken);

        return finishedBookings.Count;
    }
}