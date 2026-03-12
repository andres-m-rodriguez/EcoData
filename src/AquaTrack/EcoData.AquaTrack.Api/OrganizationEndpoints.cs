using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using static EcoData.AquaTrack.Contracts.Permissions;

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
            .WithName("GetOrganizations")
            .AllowAnonymous();

        group
            .MapGet(
                "/my",
                (ClaimsPrincipal user, IOrganizationRepository repository, CancellationToken ct) =>
                {
                    var token = new RequestClaimToken(user);
                    return token.IsAuthenticated
                        ? repository.GetMyOrganizationsAsync(token.UserId!.Value, ct)
                        : AsyncEnumerable.Empty<MyOrganizationDto>();
                }
            )
            .WithName("GetMyOrganizations")
            .RequireAuthorization();

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<OrganizationDtoForDetail>, NotFound>> (
                    Guid id,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await repository.GetByIdAsync(id, ct);
                    return organization is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(organization);
                }
            )
            .WithName("GetOrganizationById");

        group
            .MapPost(
                "/",
                async Task<Results<Created<OrganizationDtoForCreated>, Conflict<string>>> (
                    OrganizationDtoForCreate dto,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var exists = await repository.ExistsAsync(dto.Name, ct);
                    if (exists)
                    {
                        return TypedResults.Conflict(
                            "An organization with this name already exists."
                        );
                    }

                    var created = await repository.CreateAsync(dto, ct);
                    return TypedResults.Created($"/api/organizations/{created.Id}", created);
                }
            )
            .WithName("CreateOrganization")
            .RequireAuthorization(PolicyNames.Admin);

        group
            .MapPut(
                "/{id:guid}",
                async Task<Results<Ok<OrganizationDtoForDetail>, NotFound, ForbidHttpResult>> (
                    Guid id,
                    OrganizationDtoForUpdate dto,
                    ClaimsPrincipal user,
                    IOrganizationRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            id,
                            Organization.Update,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var updated = await repository.UpdateAsync(id, dto, ct);
                    return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
                }
            )
            .WithName("UpdateOrganization")
            .RequireAuthorization();

        group
            .MapDelete(
                "/{id:guid}",
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid id,
                    ClaimsPrincipal user,
                    IOrganizationRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            id,
                            Organization.Delete,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var deleted = await repository.DeleteAsync(id, ct);
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("DeleteOrganization")
            .RequireAuthorization();

        return app;
    }
}
