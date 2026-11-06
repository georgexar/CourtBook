namespace CourtBook.Application.Bookings;

public record CreateBookingRequest(
    Guid CourtId,
    Guid UserId,   // TODO (Φάση 4): αφαίρεση. Θα έρχεται από το JWT του συνδεδεμένου χρήστη.
    DateTimeOffset Start,
    DateTimeOffset End);