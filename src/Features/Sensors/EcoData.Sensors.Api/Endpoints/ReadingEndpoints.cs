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
    private const string SurfaceWaterStationsCacheKey = "readings:topics:surface-water:stations";
    private const string SurfaceWaterMarkersCacheKey = "readings:topics:surface-water:markers";
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
                async (
                    [AsParameters] SurfaceWaterStationParameters parameters,
                    ISurfaceWaterRepository repository,
                    IMemoryCache cache,
                    CancellationToken ct
                ) =>
                {
                    // The sorted list is identical for every page, so it is cached
                    // whole and sliced per request.
                    var sorted = await cache.GetOrCreateAsync(
                        SurfaceWaterStationsCacheKey,
                        entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = TopicCacheTtl;
                            return repository.GetSortedStationsAsync(ct);
                        }
                    ) ?? [];

                    var startIndex = 0;
                    if (parameters.Cursor.HasValue)
                    {
                        var idx = sorted.FindIndex(s => s.SensorId == parameters.Cursor.Value);
                        startIndex = idx >= 0 ? idx + 1 : 0;
                    }

                    var pageSize = parameters.PageSize > 0 ? parameters.PageSize : 50;
                    return sorted.Skip(startIndex).Take(pageSize);
                }
            )
            .WithName("GetSurfaceWaterStations");

        topics
            .MapGet(
                "/surface-water/stations/markers",
                (ISurfaceWaterRepository repository, IMemoryCache cache, CancellationToken ct) =>
                    cache.GetOrCreateAsync(
                        SurfaceWaterMarkersCacheKey,
                        entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = TopicCacheTtl;
                            return repository.GetMarkersAsync(ct);
                        }
                    )
            )
            .WithName("GetSurfaceWaterStationMarkers");

        return app;
    }
}
