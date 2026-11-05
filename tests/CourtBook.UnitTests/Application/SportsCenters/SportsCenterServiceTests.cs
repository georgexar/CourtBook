using CourtBook.Application.Common.Exceptions;
using CourtBook.Application.SportsCenters;
using CourtBook.Domain.Common;
using CourtBook.Domain.Enums;
using CourtBook.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using CourtBook.Domain.Entities;

namespace CourtBook.UnitTests.Application.SportsCenters;

public sealed class SportsCenterServiceTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider = new(DomainFactory.Now);
    private readonly SqliteTestDatabase _database;

    public SportsCenterServiceTests()
    {
        _database = new SqliteTestDatabase(_timeProvider);
    }

    public void Dispose() => _database.Dispose();

    private static CreateSportsCenterRequest ValidRequest(string name = "Piraeus Sports Center") =>
        new(name, "Akti Miaouli 10", "Piraeus", "2101234567");

    // ---------- Βοηθητικές μέθοδοι ----------

    // Arrange: αποθηκεύει κέντρα στη βάση (σαν να υπήρχαν ήδη)
    private async Task SaveAsync(params SportsCenter[] centers)
    {
        await using var context = _database.CreateContext();
        context.SportsCenters.AddRange(centers);
        await context.SaveChangesAsync();
    }

    // Act: τρέχει κάτι στο service, με δικό του context (σαν ένα HTTP request)
    private async Task RunServiceAsync(Func<SportsCenterService, Task> action)
    {
        await using var context = _database.CreateContext();
        await action(new SportsCenterService(context));
    }

    // Assert: διαβάζει ένα γήπεδο φρέσκο από τη βάση
    private async Task<Court> FindCourtAsync(Guid courtId)
    {
        await using var context = _database.CreateContext();
        return await context.Courts.SingleAsync(c => c.Id == courtId);
    }


    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsSportsCenter()
    {
        // Act
        Guid id;
        await using (var context = _database.CreateContext())
        {
            var service = new SportsCenterService(context);
            id = await service.CreateAsync(ValidRequest());
        }

        // Assert: με ΝΕΟ context, για να διαβάσουμε από τη βάση
        await using var assertContext = _database.CreateContext();
        var saved = await assertContext.SportsCenters.SingleAsync(c => c.Id == id);

        Assert.Equal("Piraeus Sports Center", saved.Name);
        Assert.Equal("Piraeus", saved.City);
        Assert.Equal(DomainFactory.Now, saved.CreatedAt);
        Assert.Null(saved.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsDomainExceptionAndSavesNothing()
    {
        // Arrange
        await using var context = _database.CreateContext();
        var service = new SportsCenterService(context);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(ValidRequest(name: "")));
        Assert.False(await context.SportsCenters.AnyAsync());
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_ReturnsCentersOrderedByNameWithCourtCount()
    {
        // Arrange
        await using (var context = _database.CreateContext())
        {
            var beta = DomainFactory.CreateSportsCenter(name: "Beta Club");
            beta.AddCourt("Padel 1", SportType.Padel);
            beta.AddCourt("Padel 2", SportType.Padel);

            var alpha = DomainFactory.CreateSportsCenter(name: "Alpha Club");

            context.SportsCenters.AddRange(beta, alpha);
            await context.SaveChangesAsync();
        }

        // Act
        await using var actContext = _database.CreateContext();
        var result = await new SportsCenterService(actContext).GetAllAsync();

        // Assert
        Assert.Collection(result,
            first =>
            {
                Assert.Equal("Alpha Club", first.Name);
                Assert.Equal(0, first.CourtCount);
            },
            second =>
            {
                Assert.Equal("Beta Club", second.Name);
                Assert.Equal(2, second.CourtCount);
            });
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_WhenCenterDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = _database.CreateContext();
        var service = new SportsCenterService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    // ---------- AddCourtAsync ----------

    [Fact]
    public async Task AddCourtAsync_WhenCenterExists_PersistsCourt()
    {
        // Arrange
        var center = DomainFactory.CreateSportsCenter();
        await using (var context = _database.CreateContext())
        {
            context.SportsCenters.Add(center);
            await context.SaveChangesAsync();
        }

        // Act
        Guid courtId;
        await using (var context = _database.CreateContext())
        {
            var service = new SportsCenterService(context);
            courtId = await service.AddCourtAsync(center.Id, new AddCourtRequest("Padel 1", SportType.Padel));
        }

        // Assert
        await using var assertContext = _database.CreateContext();
        var court = await assertContext.Courts.SingleAsync(c => c.Id == courtId);

        Assert.Equal(center.Id, court.SportsCenterId);
        Assert.Equal("Padel 1", court.Name);
        Assert.True(court.IsActive);
    }

    [Fact]
    public async Task AddCourtAsync_WhenCenterDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        await using var context = _database.CreateContext();
        var service = new SportsCenterService(context);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddCourtAsync(Guid.NewGuid(), new AddCourtRequest("Padel 1", SportType.Padel)));
    }

    [Fact]
    public async Task AddCourtAsync_WithDuplicateName_ThrowsDomainException()
    {
        // Arrange: κέντρο που έχει ήδη το "Padel 1" στη βάση
        var center = DomainFactory.CreateSportsCenter();
        center.AddCourt("Padel 1", SportType.Padel);
        await using (var context = _database.CreateContext())
        {
            context.SportsCenters.Add(center);
            await context.SaveChangesAsync();
        }

        // Act & Assert: με νέο context, όπως σε ένα νέο HTTP request
        await using var actContext = _database.CreateContext();
        var service = new SportsCenterService(actContext);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.AddCourtAsync(center.Id, new AddCourtRequest("padel 1", SportType.Padel)));
    }

    // ---------- SetCourtActiveAsync ----------

    [Fact]
    public async Task SetCourtActiveAsync_Deactivate_PersistsInactiveCourtAndSetsUpdatedAt()
    {
        // Arrange: ένα κέντρο με ένα γήπεδο, αποθηκευμένο στη βάση
        var center = DomainFactory.CreateSportsCenter();
        var court = center.AddCourt("Padel 1", SportType.Padel);
        await SaveAsync(center);

        _timeProvider.Advance(TimeSpan.FromHours(1));   // πέρασε μία ώρα

        // Act: απενεργοποίηση
        await RunServiceAsync(service =>
            service.SetCourtActiveAsync(center.Id, court.Id, isActive: false));

        // Assert: τι λέει η βάση;
        var saved = await FindCourtAsync(court.Id);
        Assert.False(saved.IsActive);
        Assert.Equal(DomainFactory.Now, saved.CreatedAt);
        Assert.Equal(DomainFactory.Now.AddHours(1), saved.UpdatedAt);
    }

    [Fact]
    public async Task SetCourtActiveAsync_WhenCourtDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange: ένα κέντρο χωρίς το γήπεδο που θα ζητήσουμε
        var center = DomainFactory.CreateSportsCenter();
        await SaveAsync(center);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            RunServiceAsync(service =>
                service.SetCourtActiveAsync(center.Id, Guid.NewGuid(), isActive: false)));
    }

    [Fact]
    public async Task SetCourtActiveAsync_WhenCourtBelongsToAnotherCenter_ThrowsNotFoundException()
    {
        // Arrange: δύο κέντρα, το γήπεδο ανήκει στο A
        var centerA = DomainFactory.CreateSportsCenter(name: "Center A");
        var court = centerA.AddCourt("Padel 1", SportType.Padel);
        var centerB = DomainFactory.CreateSportsCenter(name: "Center B");
        await SaveAsync(centerA, centerB);

        // Act & Assert: προσπαθούμε μέσω του B
        await Assert.ThrowsAsync<NotFoundException>(() =>
            RunServiceAsync(service =>
                service.SetCourtActiveAsync(centerB.Id, court.Id, isActive: false)));

        // και το γήπεδο ΔΕΝ άλλαξε
        var unchanged = await FindCourtAsync(court.Id);
        Assert.True(unchanged.IsActive);
    }



}