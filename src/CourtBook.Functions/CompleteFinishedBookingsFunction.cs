using CourtBook.Application.Bookings;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CourtBook.Functions;

public sealed class CompleteFinishedBookingsFunction(
    IBookingMaintenanceService maintenanceService,
    ILogger<CompleteFinishedBookingsFunction> logger)
{
    [Function("CompleteFinishedBookings")]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        var completed = await maintenanceService.CompleteFinishedBookingsAsync(cancellationToken);

        logger.LogInformation("Completed {Count} finished bookings.", completed);
    }
}