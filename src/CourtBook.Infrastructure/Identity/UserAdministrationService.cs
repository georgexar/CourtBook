using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.Common.Interfaces;
using CourtBook.Application.Common.Security;
using CourtBook.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace CourtBook.Infrastructure.Identity;

internal sealed class UserAdministrationService(
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser) : IUserAdministrationService
{
    public async Task<UserResponse> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException($"User '{email}' was not found.");

        return await ToResponseAsync(user);
    }

    public async Task AddRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        EnsureRoleExists(role);
        var user = await FindUserAsync(userId);

        if (await userManager.IsInRoleAsync(user, role))
            return;   // έχει ήδη τον ρόλο: τίποτα να κάνουμε

        var result = await userManager.AddToRoleAsync(user, role);
        ThrowIfFailed(result);
    }

    public async Task RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        EnsureRoleExists(role);

        if (userId == currentUser.UserId && role == Roles.Admin)
            throw new ValidationFailedException(["You cannot remove the Admin role from yourself."]);

        var user = await FindUserAsync(userId);

        if (!await userManager.IsInRoleAsync(user, role))
            return;   // δεν έχει τον ρόλο: τίποτα να κάνουμε

        var result = await userManager.RemoveFromRoleAsync(user, role);
        ThrowIfFailed(result);
    }

    // ---------- Βοηθητικές ----------

    private async Task<ApplicationUser> FindUserAsync(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException($"User '{userId}' was not found.");
    }

    private async Task<UserResponse> ToResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(user.Id, user.Email!, user.FullName, roles.ToList());
    }

    private static void EnsureRoleExists(string role)
    {
        if (!Roles.All.Contains(role))
            throw new ValidationFailedException([$"Role '{role}' does not exist. Valid roles: {string.Join(", ", Roles.All)}."]);
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ValidationFailedException(result.Errors.Select(e => e.Description).ToList());
    }
}