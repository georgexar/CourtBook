namespace CourtBook.Application.Bookings;

public interface IBookingService
{
    Task<Guid> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}