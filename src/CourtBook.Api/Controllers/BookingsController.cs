using CourtBook.Application.Bookings;
using Microsoft.AspNetCore.Mvc;

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
}