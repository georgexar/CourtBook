namespace CourtBook.Application.SportsCenters;

public record SetOpeningHoursRequest(IReadOnlyList<OpeningHoursDto> Days);