using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class SpeciesCategoryEndpoints
{
    public static IEndpointRouteBuilder MapSpeciesCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/species-categories").WithTags("Species Categories");

        group
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<SpeciesCategoryDtoForList>>> (
                    ISpeciesCategoryRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var categories = await repository.GetAllAsync(ct);
                    return TypedResults.Ok(categories);
                }
            )
            .WithName("GetSpeciesCategories");

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SpeciesCategoryDtoForDetail>, NotFound>> (
                    Guid id,
                    ISpeciesCategoryRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var category = await repository.GetByIdAsync(id, ct);
                    return category is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(category);
                }
            )
            .WithName("GetSpeciesCategoryById");

        group
            .MapGet(
                "/by-code/{code}",
                async Task<Results<Ok<SpeciesCategoryDtoForDetail>, NotFound>> (
                    string code,
                    ISpeciesCategoryRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var category = await repository.GetByCodeAsync(code, ct);
                    return category is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(category);
                }
            )
            .WithName("GetSpeciesCategoryByCode");

        return app;
    }
}
