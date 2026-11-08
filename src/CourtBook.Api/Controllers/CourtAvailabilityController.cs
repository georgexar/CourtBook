using CourtBook.Application.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/courts/{courtId:guid}/availability")]
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