namespace CourtBook.Application.Bookings.Rules;

internal sealed class NoOverlapRule : IBookingRule
{
    public string? Validate(BookingRuleContext context)
    {
        var isAlreadyBooked = context.ExistingBookingsOnCourt
            .Any(b => b.IsActive && b.OverlapsWith(context.Start, context.End));

        return isAlreadyBooked
            ? "The court is already booked for this time."
            : null;
    }
}