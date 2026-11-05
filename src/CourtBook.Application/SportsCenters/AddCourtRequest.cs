using CourtBook.Domain.Enums;

namespace CourtBook.Application.SportsCenters;

public record AddCourtRequest(string Name, SportType SportType);