namespace CourtBook.Application.SportsCenters;

public record CreateSportsCenterRequest(
    string Name,
    string Address,
    string City,
    string? PhoneNumber);