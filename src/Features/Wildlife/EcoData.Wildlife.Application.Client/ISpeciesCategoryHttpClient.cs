using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface ISpeciesCategoryHttpClient
{
    Task<OneOf<IReadOnlyList<SpeciesCategoryDtoForList>, RequestFailed>> GetAllAsync(
        CancellationToken ct = default);

    Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OneOf<SpeciesCategoryDtoForDetail, RequestFailed>> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<TaxonFacetDto>, RequestFailed>> GetCountsAsync(CancellationToken ct = default);
}
