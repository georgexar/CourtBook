using CourtBook.Application.Auth;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.Common.Security;
using CourtBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CourtBook.Infrastructure.Authentication;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName?.Trim() ?? string.Empty
        };

        // ① Δημιουργία: το Identity ελέγχει τους κανόνες και κάνει hash τον κωδικό
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new ValidationFailedException(result.Errors.Select(e => e.Description).ToList());

        // ② Κάθε νέος χρήστης είναι Player
        await userManager.AddToRoleAsync(user, Roles.Player);

        return await CreateResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("Account is temporarily locked. Try again later.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);   // μετράει τις λάθος προσπάθειες
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await CreateResponseAsync(user);
    }

    private async Task<AuthResponse> CreateResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAt) = tokenService.CreateToken(user, roles);

        return new AuthResponse(token, expiresAt, user.Id, user.Email!, roles.ToList());
    }
}