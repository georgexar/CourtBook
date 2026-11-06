using CourtBook.Domain.Entities;

namespace CourtBook.Application.Bookings.Rules;

public sealed record BookingRuleContext(
    SportsCenter Center,
    Court Court,
    Guid UserId,
    DateTimeOffset Start,
    DateTimeOffset End,
    IReadOnlyCollection<Booking> ExistingBookingsOnCourt,
    int UserActiveBookingsCount,
    TimeZoneInfo TimeZone)
{
    public DateTimeOffset LocalStart => TimeZoneInfo.ConvertTime(Start, TimeZone);
    public DateTimeOffset LocalEnd => TimeZoneInfo.ConvertTime(End, TimeZone);
}