namespace CourtBook.Application.SportsCenters;

public record OpeningHoursDto(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt);