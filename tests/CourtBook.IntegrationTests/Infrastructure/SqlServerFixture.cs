using CourtBook.Application;
using CourtBook.Application.Common.Interfaces;
using CourtBook.Infrastructure;
using CourtBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace CourtBook.IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private ServiceProvider _serviceProvider = null!;

    public IServiceProvider Services => _serviceProvider;

    public async Task InitializeAsync()
    {
        // ① Σηκώνει έναν καθαρό SQL Server σε Docker
        await _container.StartAsync();

        // ② Συναρμολογεί την εφαρμογή με το ΠΡΑΓΜΑΤΙΚΟ DI
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(_container.GetConnectionString());

        // ③ Ψεύτικος χρήστης: ένας ανά scope («request»)
        services.AddScoped<FakeCurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<FakeCurrentUser>());

        _serviceProvider = services.BuildServiceProvider();

        // ④ Τρέχει τα ΠΡΑΓΜΑΤΙΚΑ migrations
        using var scope = _serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _container.DisposeAsync();
    }
}