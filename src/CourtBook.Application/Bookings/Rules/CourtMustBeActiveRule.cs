namespace CourtBook.Application.Bookings.Rules;

internal sealed class CourtMustBeActiveRule : IBookingRule
{
    public string? Validate(BookingRuleContext context)
    {
        return context.Court.IsActive
            ? null
            : $"Court '{context.Court.Name}' is not available for booking.";
    }
}