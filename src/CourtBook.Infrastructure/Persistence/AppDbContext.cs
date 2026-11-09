using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CourtBook.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CourtBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CourtBook.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    public DbSet<SportsCenter> SportsCenters => Set<SportsCenter>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Το SQLite (που χρησιμοποιούμε στα tests) δεν συγκρίνει DateTimeOffset.
        // Τα αποθηκεύουμε ως αριθμούς, ώστε τα queries με ώρες να δουλεύουν.
        // Στον SQL Server αυτό ΔΕΝ εφαρμόζεται.
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var dateTimeOffsetProperties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

                foreach (var property in dateTimeOffsetProperties)
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
            }
        }
    }
}
