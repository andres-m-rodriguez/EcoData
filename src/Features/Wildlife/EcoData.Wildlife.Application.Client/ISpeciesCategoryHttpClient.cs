using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public interface ISpeciesCategoryHttpClient
{
    Task<IReadOnlyList<SpeciesCategoryDtoForList>> GetAllAsync(CancellationToken ct = default);

    Task<SpeciesCategoryDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SpeciesCategoryDtoForDetail?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<TaxonFacetDto>> GetCountsAsync(CancellationToken ct = default);
}
