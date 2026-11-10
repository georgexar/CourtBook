namespace CourtBook.Application.Users;

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);