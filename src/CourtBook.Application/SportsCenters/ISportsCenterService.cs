namespace CourtBook.Application.SportsCenters;

public interface ISportsCenterService
{
    Task<Guid> CreateAsync(CreateSportsCenterRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SportsCenterResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SportsCenterDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}