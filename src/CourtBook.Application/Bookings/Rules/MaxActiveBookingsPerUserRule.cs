namespace CourtBook.Application.Bookings.Rules;

internal sealed class MaxActiveBookingsPerUserRule : IBookingRule
{
    public const int MaxActiveBookings = 3;

    public string? Validate(BookingRuleContext context)
    {
        return context.UserActiveBookingsCount >= MaxActiveBookings
            ? $"You cannot have more than {MaxActiveBookings} active bookings."
            : null;
    }
}