namespace CourtBook.Application.Bookings.Rules;

internal sealed class AllowedDurationRule : IBookingRule
{
    internal static readonly TimeSpan[] AllowedDurations =
    [
        TimeSpan.FromMinutes(60),
        TimeSpan.FromMinutes(90)
    ];

    public string? Validate(BookingRuleContext context)
    {
        var duration = context.End - context.Start;

        return AllowedDurations.Contains(duration)
            ? null
            : "A booking must last 60 or 90 minutes.";
    }
}