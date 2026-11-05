using CourtBook.Domain.Common;

namespace CourtBook.Domain.ValueObjects;

public sealed record DailyOpeningHours
{
    public DayOfWeek DayOfWeek { get; private init; }
    public TimeOnly OpensAt { get; private init; }
    public TimeOnly ClosesAt { get; private init; }

    // Για το EF Core
    private DailyOpeningHours() { }

    public DailyOpeningHours(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        DayOfWeek = Guard.ValidEnum(dayOfWeek, nameof(DayOfWeek));

        if (closesAt <= opensAt)
            throw new DomainException($"Closing time must be after opening time on {dayOfWeek}.");

        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    public bool Covers(TimeOnly start, TimeOnly end)
    {
        return start < end
            && start >= OpensAt
            && end <= ClosesAt;
    }
}