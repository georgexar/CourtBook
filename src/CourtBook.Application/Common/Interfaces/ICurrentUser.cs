namespace CourtBook.Application.Common.Interfaces;

public interface ICurrentUser
{
    /// <summary>Το Id του συνδεδεμένου χρήστη. Πετάει UnauthorizedException αν δεν υπάρχει.</summary>
    Guid UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}