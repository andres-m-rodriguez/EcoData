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
        var group = app.MapGroup("/api/species").WithTags("Species");

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
                "/{id:guid}/image",
                async Task<Results<FileContentHttpResult, NotFound>> (
                    Guid id,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var imageData = await repository.GetProfileImageAsync(id, ct);
                    return imageData is null
                        ? TypedResults.NotFound()
                        : TypedResults.File(imageData, "image/jpeg");
                }
            )
            .WithName("GetSpeciesImage");

        group
            .MapGet(
                "/by-municipality/{municipalityId:guid}",
                async Task<Ok<IReadOnlyList<SpeciesDtoForList>>> (
                    Guid municipalityId,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetByMunicipalityAsync(municipalityId, ct);
                    return TypedResults.Ok(species);
                }
            )
            .WithName("GetSpeciesByMunicipality");

        group
            .MapGet(
                "/by-category/{categoryId:guid}",
                async Task<Ok<IReadOnlyList<SpeciesDtoForList>>> (
                    Guid categoryId,
                    ISpeciesRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var species = await repository.GetByCategoryAsync(categoryId, ct);
                    return TypedResults.Ok(species);
                }
            )
            .WithName("GetSpeciesByCategory");

        return app;
    }
}
