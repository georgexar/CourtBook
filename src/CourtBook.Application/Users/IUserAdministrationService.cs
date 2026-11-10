namespace CourtBook.Application.Users;

public interface IUserAdministrationService
{
    Task<UserResponse> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}