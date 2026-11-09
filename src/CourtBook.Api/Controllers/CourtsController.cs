using CourtBook.Application.SportsCenters;
using Microsoft.AspNetCore.Mvc;
using CourtBook.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;


namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/centers/{centerId:guid}/courts")]
[Authorize(Roles = Roles.ManagerOrAdmin)]
public class CourtsController(ISportsCenterService sportsCenterService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid centerId, AddCourtRequest request, CancellationToken cancellationToken)
    {
        var courtId = await sportsCenterService.AddCourtAsync(centerId, request, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(SportsCentersController.GetById),
            controllerName: "SportsCenters",
            routeValues: new { id = centerId },
            value: new { id = courtId });
    }

    [HttpPost("{courtId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid centerId, Guid courtId, CancellationToken cancellationToken)
    {
        await sportsCenterService.SetCourtActiveAsync(centerId, courtId, isActive: false, cancellationToken);

        return NoContent();
    }

    [HttpPost("{courtId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid centerId, Guid courtId, CancellationToken cancellationToken)
    {
        await sportsCenterService.SetCourtActiveAsync(centerId, courtId, isActive: true, cancellationToken);

        return NoContent();
    }
}