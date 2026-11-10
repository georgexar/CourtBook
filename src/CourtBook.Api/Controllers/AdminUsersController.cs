using CourtBook.Application.Common.Security;
using CourtBook.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(IUserAdministrationService userAdministration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FindByEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        var user = await userAdministration.FindByEmailAsync(email, cancellationToken);

        return Ok(user);
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await userAdministration.AddRoleAsync(id, request.Role, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, string role, CancellationToken cancellationToken)
    {
        await userAdministration.RemoveRoleAsync(id, role, cancellationToken);

        return NoContent();
    }
}