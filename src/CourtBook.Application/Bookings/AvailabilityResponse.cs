namespace CourtBook.Application.Bookings;

public record AvailabilityResponse(
    Guid CourtId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyList<TimeSlotResponse> Slots);

public record TimeSlotResponse(
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAvailable);