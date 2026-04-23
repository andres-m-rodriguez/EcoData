using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface ISpeciesRepository
{
    Task<SpeciesDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<int> GetCountAsync(SpeciesParameters parameters, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesDtoForList>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesDtoForList>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );

    Task<byte[]?> GetProfileImageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SpeciesStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<SpeciesFacetsDto> GetFacetsAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(
        CancellationToken cancellationToken = default
    );
}
