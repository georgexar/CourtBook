using CourtBook.Domain.Enums;

namespace CourtBook.Application.Bookings;

public record BookingResponse(
    Guid Id,
    Guid CourtId,
    Guid UserId,
    DateTimeOffset Start,
    DateTimeOffset End,
    BookingStatus Status,
    decimal Price,
    string Currency,
    bool IsLateCancellation);