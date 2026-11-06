namespace CourtBook.Application.Bookings.Rules;

public interface IBookingRule
{
    /// <summary>
    /// Επιστρέφει null αν ο κανόνας τηρείται, αλλιώς ένα μήνυμα σφάλματος.
    /// </summary>
    string? Validate(BookingRuleContext context);
}