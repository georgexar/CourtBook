namespace CourtBook.Application.Bookings;

public interface IBookingMaintenanceService
{
    /// <summary>Σημειώνει ως Completed τις επιβεβαιωμένες κρατήσεις που έχουν λήξει. Επιστρέφει πόσες άλλαξαν.</summary>
    Task<int> CompleteFinishedBookingsAsync(CancellationToken cancellationToken = default);
}