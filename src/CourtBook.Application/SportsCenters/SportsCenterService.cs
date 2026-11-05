using CourtBook.Application.Common.Interfaces;
using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}