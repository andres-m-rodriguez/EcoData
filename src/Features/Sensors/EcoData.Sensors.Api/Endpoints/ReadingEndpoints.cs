using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace EcoData.Sensors.Api.Endpoints;

public static class ReadingEndpoints
{
    private const string TotalCountCacheKey = "readings:total-count";
    private const string SurfaceWaterSummaryCacheKey = "readings:topics:surface-water:summary";
    private static readonly TimeSpan TotalCountCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TopicCacheTtl = TimeSpan.FromMinutes(2);

    public static IEndpointRouteBuilder MapReadingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/readings").WithTags("Readings");

        group
            .MapGet(
                "/count",
                (IReadingRepository repository, IMemoryCache cache, CancellationToken ct) =>
                    cache.GetOrCreateAsync(
                        TotalCountCacheKey,
                        entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = TotalCountCacheTtl;
                            return repository.GetTotalCountAsync(ct);
                        }
                    )
            )
            .WithName("GetTotalReadingCount");

        var topics = group.MapGroup("/topics");

        topics
            .MapGet(
                "/surface-water/summary",
                (ISurfaceWaterRepository repository, IMemoryCache cache, CancellationToken ct) =>
                    cache.GetOrCreateAsync(
                        SurfaceWaterSummaryCacheKey,
                        entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = TopicCacheTtl;
                            return repository.GetSummaryAsync(ct);
                        }
                    )
            )
            .WithName("GetSurfaceWaterSummary");

        topics
            .MapGet(
                "/surface-water/stations",
                (
                    [AsParameters] SurfaceWaterStationParameters parameters,
                    ISurfaceWaterRepository repository,
                    CancellationToken ct
                ) => repository.GetStationsAsync(parameters, ct)
            )
            .WithName("GetSurfaceWaterStations");

        topics
            .MapGet(
                "/surface-water/stations/markers",
                (ISurfaceWaterRepository repository, CancellationToken ct) =>
                    repository.GetMarkersAsync(ct)
            )
            .WithName("GetSurfaceWaterStationMarkers");

        return app;
    }
}
