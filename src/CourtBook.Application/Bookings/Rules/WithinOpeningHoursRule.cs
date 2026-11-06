namespace CourtBook.Application.Bookings.Rules;

internal sealed class WithinOpeningHoursRule : IBookingRule
{
    public string? Validate(BookingRuleContext context)
    {
        var localStart = context.LocalStart;
        var localEnd = context.LocalEnd;

        if (localStart.Date != localEnd.Date)
            return "A booking cannot extend past midnight.";

        var hours = context.Center.GetOpeningHoursFor(localStart.DayOfWeek);

        if (hours is null)
            return $"The sports center is closed on {localStart.DayOfWeek}.";

        var start = TimeOnly.FromDateTime(localStart.DateTime);
        var end = TimeOnly.FromDateTime(localEnd.DateTime);

        return hours.Covers(start, end)
            ? null
            : $"The booking must be within opening hours ({hours.OpensAt:HH:mm}–{hours.ClosesAt:HH:mm}).";
    }
}