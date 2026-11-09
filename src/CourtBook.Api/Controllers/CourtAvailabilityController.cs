using CourtBook.Application.Bookings;
using Microsoft.AspNetCore.Mvc;
using CourtBook.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;

namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/courts/{courtId:guid}/availability")]
[AllowAnonymous]
public class CourtAvailabilityController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid courtId,
        [FromQuery] DateOnly date,
        [FromQuery] int duration = 90,
        CancellationToken cancellationToken = default)
    {
        var availability = await bookingService.GetAvailabilityAsync(courtId, date, duration, cancellationToken);

        return Ok(availability);
    }
}