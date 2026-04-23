using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface ISpeciesCategoryRepository
{
    Task<IReadOnlyList<SpeciesCategoryDtoForList>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SpeciesCategoryDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SpeciesCategoryDtoForDetail?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxonFacetDto>> GetCountsAsync(CancellationToken cancellationToken = default);
}
