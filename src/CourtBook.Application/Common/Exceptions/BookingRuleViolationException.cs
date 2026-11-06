namespace CourtBook.Application.Common.Exceptions;

public sealed class BookingRuleViolationException(IReadOnlyList<string> errors)
    : Exception("The booking violates one or more rules.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}