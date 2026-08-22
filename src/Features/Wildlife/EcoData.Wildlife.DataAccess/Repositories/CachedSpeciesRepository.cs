using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EcoData.Wildlife.DataAccess.Repositories;

// The catalogue stats are six aggregate queries over the whole species table,
// asked for by every landing of the Species and Municipios pages and by the MCP
// tool. Nothing in this process writes species — the seeder and backfills run
// out of process — so a short time-based cache is safe and needs no invalidation.
// Everything else passes straight through.
public sealed class CachedSpeciesRepository(SpeciesRepository inner, IMemoryCache cache) : ISpeciesRepository
{
    private const string StatsKey = "wildlife:species:stats";
    private static readonly TimeSpan StatsLifetime = TimeSpan.FromMinutes(15);

    public async Task<SpeciesStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await cache.GetOrCreateAsync(
            StatsKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = StatsLifetime;
                return inner.GetStatsAsync(cancellationToken);
            }
        );

        // GetOrCreateAsync only yields null when the factory does, which it never does here.
        return stats!;
    }

    public Task<SpeciesDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.GetByIdAsync(id, cancellationToken);

    public IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    ) => inner.GetSpeciesAsync(parameters, cancellationToken);

    public Task<int> GetCountAsync(SpeciesParameters parameters, CancellationToken cancellationToken = default) =>
        inner.GetCountAsync(parameters, cancellationToken);

    public Task<byte[]?> GetProfileImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.GetProfileImageAsync(id, cancellationToken);

    public Task<SpeciesFacetsDto> GetFacetsAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    ) => inner.GetFacetsAsync(parameters, cancellationToken);

    public Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(CancellationToken cancellationToken = default) =>
        inner.GetFeaturedAsync(cancellationToken);

    public Task<IReadOnlyList<MunicipalitySpeciesCountDto>> GetCountsByMunicipalityAsync(
        CancellationToken cancellationToken = default
    ) => inner.GetCountsByMunicipalityAsync(cancellationToken);

    public Task<IReadOnlyList<SpeciesNearbyDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken cancellationToken = default
    ) => inner.GetNearbyAsync(latitude, longitude, radiusMeters, cancellationToken);

    public Task<IReadOnlyList<SpeciesNearbyDto>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken cancellationToken = default
    ) => inner.GetInPolygonAsync(coordinates, cancellationToken);

    public Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(CancellationToken cancellationToken = default) =>
        inner.GetHeatmapAsync(cancellationToken);
}
