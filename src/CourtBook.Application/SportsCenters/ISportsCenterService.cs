namespace CourtBook.Application.SportsCenters;

public interface ISportsCenterService
{
    Task<Guid> CreateAsync(CreateSportsCenterRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SportsCenterResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SportsCenterDetailsResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> AddCourtAsync(Guid centerId, AddCourtRequest request, CancellationToken cancellationToken = default);

    Task SetCourtActiveAsync(Guid centerId, Guid courtId, bool isActive, CancellationToken cancellationToken = default);
    Task SetOpeningHoursAsync(Guid centerId, SetOpeningHoursRequest request, CancellationToken cancellationToken = default);

}