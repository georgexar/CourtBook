using CourtBook.Application.Bookings.Rules;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.UnitTests.TestData;

public static class BookingRuleContextFactory
{
    public static readonly TimeZoneInfo Athens = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

    // Πέμπτη 12/11/2026, 19:00 ώρα Ελλάδας
    public static readonly DateTimeOffset DefaultStart = new(2026, 11, 12, 19, 0, 0, TimeSpan.FromHours(2));

    /// <summary>
    /// Ένα «κανονικό» context που περνάει ΟΛΟΥΣ τους κανόνες:
    /// κέντρο ανοιχτό κάθε μέρα 08:00–23:00, ενεργό γήπεδο,
    /// κράτηση 90 λεπτών, χωρίς άλλες κρατήσεις.
    /// </summary>
    public static BookingRuleContext Valid()
    {
        var center = DomainFactory.CreateSportsCenter();
        center.SetOpeningHours(
            Enum.GetValues<DayOfWeek>()
                .Select(day => new DailyOpeningHours(day, new(8, 0), new(23, 0))));

        var court = center.AddCourt("Padel 1", SportType.Padel);

        return new BookingRuleContext(
            Center: center,
            Court: court,
            UserId: Guid.CreateVersion7(),
            Start: DefaultStart,
            End: DefaultStart.AddMinutes(90),
            ExistingBookingsOnCourt: [],
            UserActiveBookingsCount: 0,
            TimeZone: Athens);
    }

    /// <summary>Το ίδιο context, αλλά με κράτηση στην ώρα που δίνουμε (Πέμπτη 12/11).</summary>
    public static BookingRuleContext At(int hour, int minute, int durationMinutes)
    {
        var start = new DateTimeOffset(2026, 11, 12, hour, minute, 0, TimeSpan.FromHours(2));
        return Valid() with { Start = start, End = start.AddMinutes(durationMinutes) };
    }
}