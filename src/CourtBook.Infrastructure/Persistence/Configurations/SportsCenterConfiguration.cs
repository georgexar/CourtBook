using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtBook.Infrastructure.Persistence.Configurations;

public class SportsCenterConfiguration : IEntityTypeConfiguration<SportsCenter>
{
    public void Configure(EntityTypeBuilder<SportsCenter> builder)
    {
        builder.ToTable("SportsCenters");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasMaxLength(SportsCenter.NameMaxLength)
            .IsRequired();

        builder.Property(c => c.Address)
            .HasMaxLength(SportsCenter.AddressMaxLength)
            .IsRequired();

        builder.Property(c => c.City)
            .HasMaxLength(SportsCenter.CityMaxLength)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(SportsCenter.PhoneNumberMaxLength);

        // Σχέση ένα-προς-πολλά: ένα κέντρο έχει πολλά γήπεδα
        builder.HasMany(c => c.Courts)
            .WithOne()
            .HasForeignKey(court => court.SportsCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Το EF Core γράφει και διαβάζει τη λίστα μέσω του private πεδίου _courts
        builder.Navigation(c => c.Courts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}