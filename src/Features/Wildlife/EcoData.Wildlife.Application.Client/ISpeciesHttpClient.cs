using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface ISpeciesHttpClient
{
    IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<OneOf<int, RequestFailed>> GetCountAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<OneOf<SpeciesDtoForDetail, RequestFailed>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OneOf<SpeciesStatsDto, RequestFailed>> GetStatsAsync(CancellationToken ct = default);

    Task<OneOf<SpeciesFacetsDto, RequestFailed>> GetFacetsAsync(
        SpeciesParameters? parameters = null,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<SpeciesDtoForList>, RequestFailed>> GetFeaturedAsync(
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<SpeciesNearbyDto>, RequestFailed>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<HeatmapPointDto>, RequestFailed>> GetHeatmapAsync(
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<MunicipalitySpeciesCountDto>, RequestFailed>> GetCountsByMunicipalityAsync(
        CancellationToken ct = default);
}
