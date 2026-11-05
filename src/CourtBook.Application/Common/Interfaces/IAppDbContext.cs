using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<SportsCenter> SportsCenters { get; }
    DbSet<Court> Courts { get; }
    DbSet<Booking> Bookings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}