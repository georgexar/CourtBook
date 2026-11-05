using CourtBook.Domain.Enums;

namespace CourtBook.Application.SportsCenters;

public record SportsCenterDetailsResponse(
    Guid Id,
    string Name,
    string Address,
    string City,
    string? PhoneNumber,
    IReadOnlyList<CourtResponse> Courts,
    IReadOnlyList<OpeningHoursDto> OpeningHours);

public record CourtResponse(
    Guid Id,
    string Name,
    SportType SportType,
    bool IsActive);