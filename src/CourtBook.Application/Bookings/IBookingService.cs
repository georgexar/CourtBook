using CourtBook.Application.Common;
using CourtBook.Domain.Enums;

namespace CourtBook.Application.Bookings;

public interface IBookingService
{
    Task<Guid> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AvailabilityResponse> GetAvailabilityAsync(
       Guid courtId,
       DateOnly date,
       int durationMinutes,
       CancellationToken cancellationToken = default);

    Task CancelAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<BookingResponse>> GetForUserAsync(
       Guid userId,
       int page,
       int pageSize,
       BookingStatus? status = null,
       CancellationToken cancellationToken = default);
}