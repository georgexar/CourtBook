using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CourtBook.Application.Common.Interfaces;

namespace CourtBook.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<SportsCenter> SportsCenters => Set<SportsCenter>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
