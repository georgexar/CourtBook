namespace CourtBook.Application.Auth;

public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);