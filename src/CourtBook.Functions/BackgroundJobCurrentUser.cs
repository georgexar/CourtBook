using CourtBook.Application.Common.Interfaces;
using System;

namespace CourtBook.Functions;

/// <summary>
/// Οι εργασίες παρασκηνίου δεν εκτελούνται για λογαριασμό κάποιου χρήστη.
/// </summary>
internal sealed class BackgroundJobCurrentUser : ICurrentUser
{
    public Guid UserId =>
        throw new InvalidOperationException("Background jobs do not run on behalf of a user.");

    public bool IsAuthenticated => false;

    public bool IsInRole(string role) => false;
}