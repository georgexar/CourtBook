using CourtBook.Application.SportsCenters;
using Microsoft.Extensions.DependencyInjection;

namespace CourtBook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISportsCenterService, SportsCenterService>();

        return services;
    }
}