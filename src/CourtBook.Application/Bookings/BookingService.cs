using CourtBook.Application.Bookings.Rules;
using CourtBook.Application.Common;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.Common.Interfaces;
using CourtBook.Domain.Entities;
using CourtBook.Domain.Enums;
using CourtBook.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using CourtBook.Application.Bookings.Pricing;
using System.Linq.Expressions;

namespace CourtBook.Application.Bookings;

internal sealed class BookingService(
    IAppDbContext db,
    IEnumerable<IBookingRule> rules,
    BookingPriceCalculator priceCalculator,
    TimeProvider timeProvider) : IBookingService
{
    // Μετατροπή Booking → BookingResponse, που το EF Core μεταφράζει σε SQL
    private static readonly Expression<Func<Booking, BookingResponse>> ToResponse = b =>
        new BookingResponse(
            b.Id,
            b.CourtId,
            b.UserId,
            b.StartTime,
            b.EndTime,
            b.Status,
            b.Price.Amount,
            b.Price.Currency,
            b.IsLateCancellation);


    public async Task<Guid> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Το κέντρο που έχει αυτό το γήπεδο, με όλα τα γήπεδα και το ωράριό του
        var center = await db.SportsCenters
            .Include(c => c.Courts)
            .FirstOrDefaultAsync(c => c.Courts.Any(court => court.Id == request.CourtId), cancellationToken)
            ?? throw new NotFoundException($"Court '{request.CourtId}' was not found.");

        var court = center.Courts.Single(c => c.Id == request.CourtId);

        // 2. Ενεργές κρατήσεις του ΙΔΙΟΥ γηπέδου που «αγγίζουν» το ίδιο διάστημα
        var existingBookings = await db.Bookings
            .Where(b => b.CourtId == court.Id
                     && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                     && b.StartTime < request.End
                     && b.EndTime > request.Start)
            .ToListAsync(cancellationToken);

        // 3. Πόσες ενεργές κρατήσεις έχει ήδη ο χρήστης
        var userActiveBookings = await db.Bookings
            .CountAsync(b => b.UserId == request.UserId
                          && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
                        cancellationToken);

        // 4. Το «έντυπο» για τους κανόνες
        var context = new BookingRuleContext(
            Center: center,
            Court: court,
            UserId: request.UserId,
            Start: request.Start,
            End: request.End,
            ExistingBookingsOnCourt: existingBookings,
            UserActiveBookingsCount: userActiveBookings,
            TimeZone: AppTimeZone.Default);

        // 5. Όλοι οι κανόνες, και συλλογή ΟΛΩΝ των σφαλμάτων
        var errors = rules
            .Select(rule => rule.Validate(context))
            .OfType<string>()
            .ToList();

        if (errors.Count > 0)
            throw new BookingRuleViolationException(errors);

        // 6. Τιμή, δημιουργία, επιβεβαίωση, αποθήκευση
        var price = priceCalculator.Calculate(request.Start, request.End, AppTimeZone.Default);

        var booking = new Booking(court.Id, request.UserId, request.Start, request.End, price, timeProvider.GetUtcNow());
        booking.Confirm();

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return booking.Id;
    }

    public async Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(
       Guid courtId,
       DateOnly date,
       int durationMinutes,
       CancellationToken cancellationToken = default)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);

        if (!AllowedDurationRule.AllowedDurations.Contains(duration))
            throw new BookingRuleViolationException(["Duration must be 60 or 90 minutes."]);

        // 1. Το κέντρο με το γήπεδο και το ωράριο (μόνο ανάγνωση)
        var center = await db.SportsCenters
            .AsNoTracking()
            .Include(c => c.Courts)
            .FirstOrDefaultAsync(c => c.Courts.Any(court => court.Id == courtId), cancellationToken)
            ?? throw new NotFoundException($"Court '{courtId}' was not found.");

        var court = center.Courts.Single(c => c.Id == courtId);
        var timeZone = AppTimeZone.Default;

        // 2. ΕΝΑ query: όλες οι ενεργές κρατήσεις του γηπέδου που «αγγίζουν» αυτή τη μέρα
        var dayStart = AvailabilityCalculator.ToInstant(date, TimeOnly.MinValue, timeZone);
        var dayEnd = AvailabilityCalculator.ToInstant(date.AddDays(1), TimeOnly.MinValue, timeZone);

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.CourtId == courtId
                     && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                     && b.StartTime < dayEnd
                     && b.EndTime > dayStart)
            .ToListAsync(cancellationToken);

        // 3. Ο υπολογισμός γίνεται στη μνήμη
        IReadOnlyList<TimeSlotResponse> slots = court.IsActive
            ? AvailabilityCalculator.Calculate(
                center.GetOpeningHoursFor(date.DayOfWeek),
                date,
                duration,
                bookings,
                timeZone,
                timeProvider.GetUtcNow())
            : [];

        return new AvailabilityResponse(courtId, date, durationMinutes, slots);
    }
    public async Task CancelAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{bookingId}' was not found.");

        booking.Cancel(timeProvider.GetUtcNow());

        await db.SaveChangesAsync(cancellationToken);
    }

    public const int MaxPageSize = 50;

    public async Task<PagedResult<BookingResponse>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        BookingStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId);

        if (status is not null)
            query = query.Where(b => b.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return new PagedResult<BookingResponse>(items, page, pageSize, totalCount);
    }

}