namespace CourtBook.Infrastructure.Identity;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}