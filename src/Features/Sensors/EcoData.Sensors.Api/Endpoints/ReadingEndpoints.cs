using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class ReadingEndpoints
{
    public static IEndpointRouteBuilder MapReadingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/readings").WithTags("Readings");

        group
            .MapGet(
                "/count",
                (IReadingRepository repository, CancellationToken ct) =>
                    repository.GetTotalCountAsync(ct)
            )
            .WithName("GetTotalReadingCount");

        var topics = group.MapGroup("/topics");

        topics
            .MapGet(
                "/surface-water/summary",
                (ISurfaceWaterRepository repository, CancellationToken ct) =>
                    repository.GetSummaryAsync(ct)
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
