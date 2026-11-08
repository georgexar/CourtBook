using CourtBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CourtBook.Domain.ValueObjects;

namespace CourtBook.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Status)
            .IsRequired();

        // Υπολογιζόμενα properties: δεν αποθηκεύονται στη βάση
        builder.Ignore(b => b.Duration);
        builder.Ignore(b => b.IsActive);

        // Κάθε κράτηση δείχνει σε ένα γήπεδο
        builder.HasOne<Court>()
            .WithMany()
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        // Για γρήγορο έλεγχο διαθεσιμότητας: «κρατήσεις του γηπέδου Χ γύρω από την ώρα Υ»
        builder.HasIndex(b => new { b.CourtId, b.StartTime });

        // Για γρήγορο «οι κρατήσεις μου»
        builder.HasIndex(b => b.UserId);

        // Η τιμή είναι value object: γίνεται δύο στήλες στον πίνακα Bookings
        builder.OwnsOne(b => b.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(10, 2);

            price.Property(p => p.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3);
        });

        builder.Navigation(b => b.Price).IsRequired();

    }
}