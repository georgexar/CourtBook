using CourtBook.Domain.Common;
using CourtBook.Domain.Enums;

namespace CourtBook.Domain.Entities;

public class Booking : BaseEntity
{
    public static readonly TimeSpan LateCancellationWindow = TimeSpan.FromHours(24);

    public Guid CourtId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public bool IsLateCancellation { get; private set; }

    public TimeSpan Duration => EndTime - StartTime;

    public bool IsActive => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    // Για το EF Core
    private Booking() { }

    public Booking(Guid courtId, Guid userId, DateTimeOffset startTime, DateTimeOffset endTime, DateTimeOffset now)
    {
        CourtId = Guard.NotEmpty(courtId, nameof(CourtId));
        UserId = Guard.NotEmpty(userId, nameof(UserId));

        if (endTime <= startTime)
            throw new DomainException("End time must be after start time.");

        if (startTime <= now)
            throw new DomainException("Cannot create a booking in the past.");

        StartTime = startTime;
        EndTime = endTime;
        Status = BookingStatus.Pending;
    }

    // ---------- Μεταβάσεις κατάστασης ----------

    public void Confirm()
    {
        EnsureStatus(BookingStatus.Pending, "confirm");
        Status = BookingStatus.Confirmed;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (!IsActive)
            throw new DomainException($"Cannot cancel a booking with status {Status}.");

        if (now >= StartTime)
            throw new DomainException("Cannot cancel a booking that has already started.");

        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        IsLateCancellation = StartTime - now < LateCancellationWindow;
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureStatus(BookingStatus.Confirmed, "complete");

        if (now < EndTime)
            throw new DomainException("Cannot complete a booking before it ends.");

        Status = BookingStatus.Completed;
    }

    public void MarkAsNoShow(DateTimeOffset now)
    {
        EnsureStatus(BookingStatus.Confirmed, "mark as no-show");

        if (now < StartTime)
            throw new DomainException("Cannot mark as no-show a booking that has not started.");

        Status = BookingStatus.NoShow;
    }

    // ---------- Επικάλυψη ----------

    public bool OverlapsWith(DateTimeOffset otherStart, DateTimeOffset otherEnd)
    {
        return StartTime < otherEnd && otherStart < EndTime;
    }

    private void EnsureStatus(BookingStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} a booking with status {Status}. Expected {expected}.");
    }
}