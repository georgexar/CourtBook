using System.Security.Claims;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.Common.Interfaces;

namespace CourtBook.Api.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var subject = Principal?.FindFirst("sub")?.Value;

            return Guid.TryParse(subject, out var userId)
                ? userId
                : throw new UnauthorizedException("User is not authenticated.");
        }
    }

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}