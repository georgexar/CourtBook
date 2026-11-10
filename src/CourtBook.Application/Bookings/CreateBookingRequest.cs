namespace CourtBook.Application.Bookings;

public record CreateBookingRequest(
    Guid CourtId,
    DateTimeOffset Start,
    DateTimeOffset End);