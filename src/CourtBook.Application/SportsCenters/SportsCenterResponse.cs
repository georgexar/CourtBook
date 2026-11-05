namespace CourtBook.Application.SportsCenters;

public record SportsCenterResponse(
    Guid Id,
    string Name,
    string City,
    int CourtCount);