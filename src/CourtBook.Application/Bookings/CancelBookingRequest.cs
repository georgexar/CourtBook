namespace CourtBook.Application.Bookings;

public record CancelBookingRequest(
    Guid UserId);   // TODO (Φάση 4): αφαίρεση. Θα έρχεται από το JWT του συνδεδεμένου χρήστη.