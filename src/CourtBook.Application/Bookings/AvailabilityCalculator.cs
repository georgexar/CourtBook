using CourtBook.Domain.Entities;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.Application.Bookings;

internal static class AvailabilityCalculator
{
    public static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(30);

    public static IReadOnlyList<TimeSlotResponse> Calculate(
        DailyOpeningHours? openingHours,
        DateOnly date,
        TimeSpan duration,
        IReadOnlyCollection<Booking> bookings,
        TimeZoneInfo timeZone,
        DateTimeOffset now)
    {
        if (openingHours is null)
            return [];   // κλειστά εκείνη τη μέρα

        var slots = new List<TimeSlotResponse>();

        var opensAt = openingHours.OpensAt.ToTimeSpan();
        var closesAt = openingHours.ClosesAt.ToTimeSpan();

        for (var time = opensAt; time + duration <= closesAt; time += SlotInterval)
        {
            var start = ToInstant(date, TimeOnly.FromTimeSpan(time), timeZone);
            var end = start + duration;

            var isAvailable = start > now
                && !bookings.Any(b => b.IsActive && b.OverlapsWith(start, end));

            slots.Add(new TimeSlotResponse(start, end, isAvailable));
        }

        return slots;
    }

    /// <summary>Μετατρέπει «ημερομηνία + τοπική ώρα» σε συγκεκριμένη στιγμή (DateTimeOffset).</summary>
    public static DateTimeOffset ToInstant(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var localDateTime = date.ToDateTime(time);
        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }
}