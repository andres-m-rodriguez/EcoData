using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class SpeciesEndpoints
{
    public static IEndpointRouteBuilder MapSpeciesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/wildlife/species").WithTags("Species");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] SpeciesParameters parameters,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => repository.GetSpeciesAsync(parameters, ct)
            )
            .WithName("GetSpecies");

        group
            .MapGet(
                "/count",
                async (
                    [AsParameters] SpeciesParameters parameters,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var count = await repository.GetCountAsync(parameters, ct);
                    return TypedResults.Ok(new { count });
                }
            )
            .WithName("GetSpeciesCount");

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SpeciesDtoForDetail>, NotFound>> (
                    Guid id,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetByIdAsync(id, ct);
                    return species is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(species);
                }
            )
            .WithName("GetSpeciesById");

        group
            .MapGet(
                "/stats",
                async Task<Ok<SpeciesStatsDto>> (
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetStatsAsync(ct))
            )
            .WithName("GetSpeciesStats");

        group
            .MapGet(
                "/facets",
                async Task<Ok<SpeciesFacetsDto>> (
                    [AsParameters] SpeciesParameters parameters,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetFacetsAsync(parameters, ct))
            )
            .WithName("GetSpeciesFacets");

        group
            .MapGet(
                "/featured",
                async Task<Ok<IReadOnlyList<SpeciesDtoForList>>> (
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetFeaturedAsync(ct))
            )
            .WithName("GetFeaturedSpecies");

        group
            .MapGet(
                "/counts-by-municipality",
                async Task<Ok<IReadOnlyList<MunicipalitySpeciesCountDto>>> (
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetCountsByMunicipalityAsync(ct))
            )
            .WithName("GetSpeciesCountsByMunicipality");

        group
            .MapGet(
                "/nearby",
                async Task<Ok<IReadOnlyList<SpeciesNearbyDto>>> (
                    [AsParameters] NearbySpeciesParameters parameters,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await repository.GetNearbyAsync(
                            parameters.Latitude,
                            parameters.Longitude,
                            parameters.RadiusMeters,
                            ct
                        )
                    )
            )
            .WithName("GetSpeciesNearby");

        group
            .MapPost(
                "/in-polygon",
                async Task<Ok<IReadOnlyList<SpeciesNearbyDto>>> (
                    PolygonSearchParameters parameters,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetInPolygonAsync(parameters.Coordinates, ct))
            )
            .WithName("GetSpeciesInPolygon");

        group
            .MapGet(
                "/heatmap",
                async Task<Ok<IReadOnlyList<HeatmapPointDto>>> (
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetHeatmapAsync(ct))
            )
            .WithName("GetSpeciesHeatmap");

        return app;
    }
}
