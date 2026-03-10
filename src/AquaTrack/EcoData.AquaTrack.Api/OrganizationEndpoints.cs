using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").WithTags("Organizations");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] OrganizationParameters parameters,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) => repository.GetOrganizationsAsync(parameters, ct)
            )
            .WithName("GetOrganizations");

        group
            .MapGet(
                "/{id:guid}",
                async (Guid id, IOrganizationRepository repository, CancellationToken ct) =>
                {
                    var organization = await repository.GetByIdAsync(id, ct);
                    return organization is null ? Results.NotFound() : Results.Ok(organization);
                }
            )
            .WithName("GetOrganizationById");

        group
            .MapPost(
                "/",
                async (
                    OrganizationDtoForCreate dto,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var exists = await repository.ExistsAsync(dto.Name, ct);
                    if (exists)
                    {
                        return Results.Conflict("An organization with this name already exists.");
                    }

                    var created = await repository.CreateAsync(dto, ct);
                    return Results.Created($"/api/organizations/{created.Id}", created);
                }
            )
            .WithName("CreateOrganization")
            .RequireAuthorization(PolicyNames.Admin);

        group
            .MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    OrganizationDtoForUpdate dto,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var updated = await repository.UpdateAsync(id, dto, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
            )
            .WithName("UpdateOrganization")
            .RequireAuthorization(PolicyNames.Admin);

        group
            .MapDelete(
                "/{id:guid}",
                async (Guid id, IOrganizationRepository repository, CancellationToken ct) =>
                {
                    var deleted = await repository.DeleteAsync(id, ct);
                    return deleted ? Results.NoContent() : Results.NotFound();
                }
            )
            .WithName("DeleteOrganization")
            .RequireAuthorization(PolicyNames.Admin);

        return app;
    }
}
