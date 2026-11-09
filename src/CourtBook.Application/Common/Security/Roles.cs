namespace CourtBook.Application.Common.Security;

public static class Roles
{
    public const string Player = "Player";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Player, Manager, Admin];
}