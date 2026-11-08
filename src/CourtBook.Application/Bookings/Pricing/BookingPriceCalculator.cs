using CourtBook.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace CourtBook.Application.Bookings.Pricing;

internal sealed class BookingPriceCalculator(IOptions<PricingOptions> options)
{
    private static readonly TimeSpan Block = TimeSpan.FromMinutes(30);

    public Money Calculate(DateTimeOffset start, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        var pricing = options.Value;
        var total = 0m;

        for (var blockStart = start; blockStart < end; blockStart += Block)
        {
            var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(blockStart, timeZone).DateTime);

            var isPeak = localTime >= pricing.PeakStartsAt && localTime < pricing.PeakEndsAt;
            var hourlyRate = isPeak ? pricing.PeakHourlyRate : pricing.HourlyRate;

            total += hourlyRate / 2;   // κάθε κομμάτι είναι μισή ώρα
        }

        return new Money(total, pricing.Currency);
    }
}