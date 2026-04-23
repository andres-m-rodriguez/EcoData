using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;

namespace EcoData.Wildlife.Application.Client;

public interface ISpeciesHttpClient
{
    IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<int> GetCountAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<SpeciesDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SpeciesDtoForList>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<SpeciesDtoForList>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default);

    Task<SpeciesStatsDto?> GetStatsAsync(CancellationToken ct = default);

    Task<SpeciesFacetsDto?> GetFacetsAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MunicipalitySpeciesCountDto>> GetCountsByMunicipalityAsync(
        CancellationToken ct = default);
}
