using CourtBook.Application.SportsCenters;
using Microsoft.Extensions.DependencyInjection;
using CourtBook.Application.Bookings.Rules;
using CourtBook.Application.Bookings;
using CourtBook.Application.Bookings.Pricing;

namespace CourtBook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISportsCenterService, SportsCenterService>();
        services.AddScoped<IBookingService, BookingService>();


        // Κανόνες κράτησης (Strategy pattern): νέος κανόνας = μία γραμμή εδώ
        services.AddSingleton<IBookingRule, CourtMustBeActiveRule>();
        services.AddSingleton<IBookingRule, AllowedDurationRule>();
        services.AddSingleton<IBookingRule, WithinOpeningHoursRule>();
        services.AddSingleton<IBookingRule, NoOverlapRule>();
        services.AddSingleton<IBookingRule, MaxActiveBookingsPerUserRule>();
        services.AddSingleton<BookingPriceCalculator>();



        return services;
    }
}