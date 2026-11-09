namespace CourtBook.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName);