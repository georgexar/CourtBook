using CourtBook.Application.Common.Interfaces;
using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CourtBook.Application.Common.Exceptions;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.Application.SportsCenters;

internal sealed class SportsCenterService(IAppDbContext db) : ISportsCenterService
{
    public async Task<Guid> CreateAsync(CreateSportsCenterRequest request, CancellationToken cancellationToken = default)
    {
        var center = new SportsCenter(request.Name, request.Address, request.City, request.PhoneNumber);

        db.SportsCenters.Add(center);
        await db.SaveChangesAsync(cancellationToken);

        return center.Id;
    }

    public async Task<IReadOnlyList<SportsCenterResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.SportsCenters
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SportsCenterResponse(
                c.Id,
                c.Name,
                c.City,
                c.Courts.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<SportsCenterDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.SportsCenters
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new SportsCenterDetailsResponse(
                c.Id,
                c.Name,
                c.Address,
                c.City,
                c.PhoneNumber,
                c.Courts
                    .OrderBy(court => court.Name)
                    .Select(court => new CourtResponse(
                        court.Id,
                        court.Name,
                        court.SportType,
                        court.IsActive))
                    .ToList(),
                c.OpeningHours
                    .OrderBy(h => h.DayOfWeek)
                    .Select(h => new OpeningHoursDto(h.DayOfWeek, h.OpensAt, h.ClosesAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
    public async Task<Guid> AddCourtAsync(Guid centerId, AddCourtRequest request, CancellationToken cancellationToken = default)
    {
        var center = await GetCenterWithCourtsAsync(centerId, cancellationToken);

        var court = center.AddCourt(request.Name, request.SportType);

        await db.SaveChangesAsync(cancellationToken);

        return court.Id;
    }

    public async Task SetCourtActiveAsync(Guid centerId, Guid courtId, bool isActive, CancellationToken cancellationToken = default)
    {
        var center = await GetCenterWithCourtsAsync(centerId, cancellationToken);

        var court = center.Courts.FirstOrDefault(c => c.Id == courtId)
            ?? throw new NotFoundException($"Court '{courtId}' was not found in sports center '{centerId}'.");

        if (isActive)
            court.Activate();
        else
            court.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }


    private async Task<SportsCenter> GetCenterWithCourtsAsync(Guid centerId, CancellationToken cancellationToken)
    {
        return await db.SportsCenters
            .Include(c => c.Courts)
            .FirstOrDefaultAsync(c => c.Id == centerId, cancellationToken)
            ?? throw new NotFoundException($"Sports center '{centerId}' was not found.");
    }
    public async Task SetOpeningHoursAsync(Guid centerId, SetOpeningHoursRequest request, CancellationToken cancellationToken = default)
    {
        var center = await db.SportsCenters
            .FirstOrDefaultAsync(c => c.Id == centerId, cancellationToken)
            ?? throw new NotFoundException($"Sports center '{centerId}' was not found.");

        var schedule = request.Days
            .Select(day => new DailyOpeningHours(day.DayOfWeek, day.OpensAt, day.ClosesAt));

        center.SetOpeningHours(schedule);

        await db.SaveChangesAsync(cancellationToken);
    }

}