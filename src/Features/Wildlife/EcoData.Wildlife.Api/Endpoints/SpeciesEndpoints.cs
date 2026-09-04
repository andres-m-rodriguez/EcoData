using System.Security.Claims;
using EcoData.Common.Authorization;
using EcoData.Wildlife.Application;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using EcoData.Common.Problems;
using EcoData.Common.Problems.AspNetCore;

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
                async Task<Results<Ok<SpeciesDtoForDetail>, JsonHttpResult<EcoDataProblemDetails>>> (
                    Guid id,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetByIdAsync(id, ct);
                    return species is null
                        ? ProblemResults.NotFound($"Species {id} was not found.")
                        : TypedResults.Ok(species);
                }
            )
            .WithName("GetSpeciesById");

        group
            .MapGet(
                "/{id:guid}/image",
                async Task<Results<FileContentHttpResult, JsonHttpResult<EcoDataProblemDetails>>> (
                    Guid id,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var imageData = await repository.GetProfileImageAsync(id, ct);
                    return imageData is null
                        ? ProblemResults.NotFound($"Species {id} has no profile image.")
                        : TypedResults.File(imageData, "image/jpeg");
                }
            )
            .WithName("GetSpeciesImage");

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
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetNearbyAsync(
                        parameters.Latitude,
                        parameters.Longitude,
                        parameters.RadiusMeters,
                        ct
                    );
                    var visible = await WithAreasIfPermittedAsync(
                        species,
                        parameters.OrganizationId,
                        user,
                        auth,
                        ct
                    );
                    return TypedResults.Ok(visible);
                }
            )
            .WithName("GetSpeciesNearby");

        group
            .MapPost(
                "/in-polygon",
                async Task<Ok<IReadOnlyList<SpeciesNearbyDto>>> (
                    PolygonSearchParameters parameters,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetInPolygonAsync(parameters.Coordinates, ct);
                    var visible = await WithAreasIfPermittedAsync(
                        species,
                        parameters.OrganizationId,
                        user,
                        auth,
                        ct
                    );
                    return TypedResults.Ok(visible);
                }
            )
            .WithName("GetSpeciesInPolygon");

        return app;
    }

    // Where a species was found is a grant, not public data: anonymous callers
    // and members without it get the same list with the areas stripped.
    private static async Task<IReadOnlyList<SpeciesNearbyDto>> WithAreasIfPermittedAsync(
        IReadOnlyList<SpeciesNearbyDto> species,
        Guid? organizationId,
        ClaimsPrincipal user,
        IAuthorization auth,
        CancellationToken ct
    )
    {
        if (
            organizationId is { } id
            && user.Identity?.IsAuthenticated == true
            && await auth.HasAsync(WildlifePermissions.ViewSpeciesAreas, id, ct)
        )
            return species;

        return species.Select(s => s with { Area = null }).ToList();
    }
}
