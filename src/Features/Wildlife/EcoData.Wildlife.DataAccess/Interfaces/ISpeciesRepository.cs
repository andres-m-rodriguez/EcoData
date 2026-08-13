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

    /// <summary>
    /// Where the species' profile image is stored, or <see langword="null"/> when
    /// it has none. The bytes come from <see cref="ISpeciesImageStore"/>.
    /// </summary>
    Task<SpeciesImageReference?> GetProfileImageReferenceAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<SpeciesStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<SpeciesFacetsDto> GetFacetsAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MunicipalitySpeciesCountDto>> GetCountsByMunicipalityAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesNearbyDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesNearbyDto>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(
        CancellationToken cancellationToken = default
    );
}
