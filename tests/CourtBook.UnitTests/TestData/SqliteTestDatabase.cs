using CourtBook.Infrastructure.Persistence;
using CourtBook.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.UnitTests.TestData;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuditableEntityInterceptor _interceptor;

    public SqliteTestDatabase(TimeProvider timeProvider)
    {
        _interceptor = new AuditableEntityInterceptor(timeProvider);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_interceptor)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}