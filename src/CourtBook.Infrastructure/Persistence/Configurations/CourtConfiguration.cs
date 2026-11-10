using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourtBook.Infrastructure.Persistence.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("Courts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasMaxLength(Court.NameMaxLength)
            .IsRequired();

        builder.Property(c => c.SportType)
            .IsRequired();

        // Όχι δύο γήπεδα με το ίδιο όνομα στο ίδιο κέντρο, και στη βάση
        builder.HasIndex(c => new { c.SportsCenterId, c.Name })
            .IsUnique();

        builder.Property(c => c.BookingVersion)
       .IsConcurrencyToken();
    }
}