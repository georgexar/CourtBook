namespace CourtBook.Application.Common.Exceptions;

public sealed class ValidationFailedException(IReadOnlyList<string> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}