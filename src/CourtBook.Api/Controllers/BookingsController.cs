using CourtBook.Application.Bookings;
using Microsoft.AspNetCore.Mvc;
using CourtBook.Application.Common;
using CourtBook.Domain.Enums;
namespace CourtBook.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var id = await bookingService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetByIdAsync(id, cancellationToken);

        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        await bookingService.CancelAsync(id, request.UserId, cancellationToken);

        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<BookingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForUser(
       [FromQuery] Guid userId,   // TODO (Φάση 4): από το JWT, όχι από το query string
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10,
       [FromQuery] BookingStatus? status = null,
       CancellationToken cancellationToken = default)
    {
        var result = await bookingService.GetForUserAsync(userId, page, pageSize, status, cancellationToken);

        return Ok(result);
    }
}