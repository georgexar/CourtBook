using CourtBook.Application.SportsCenters;
using Microsoft.AspNetCore.Mvc;
using CourtBook.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;



namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/centers")]
public class SportsCentersController(ISportsCenterService sportsCenterService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> Create(CreateSportsCenterRequest request, CancellationToken cancellationToken)
    {
        var id = await sportsCenterService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SportsCenterResponse>>(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var centers = await sportsCenterService.GetAllAsync(cancellationToken);

        return Ok(centers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SportsCenterDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var center = await sportsCenterService.GetByIdAsync(id, cancellationToken);

        return center is null
            ? NotFound()
            : Ok(center);
    }

    [HttpPut("{id:guid}/opening-hours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = Roles.ManagerOrAdmin)]
    public async Task<IActionResult> SetOpeningHours(Guid id, SetOpeningHoursRequest request, CancellationToken cancellationToken)
    {
        await sportsCenterService.SetOpeningHoursAsync(id, request, cancellationToken);

        return NoContent();
    }


}