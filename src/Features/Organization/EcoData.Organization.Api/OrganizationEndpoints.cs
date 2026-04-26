using System.Security.Claims;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Organization.Contracts.Permissions;

namespace EcoData.Organization.Api;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organization/organizations").WithTags("Organizations");

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
                "/count",
                (
                    [AsParameters] OrganizationParameters parameters,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) => repository.GetOrganizationCountAsync(parameters, ct)
            )
            .WithName("GetOrganizationCount")
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
                async Task<Results<Ok<OrganizationDtoForDetail>, ProblemHttpResult>> (
                    Guid id,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await repository.GetByIdAsync(id, ct);
                    if (organization is null)
                    {
                        return TypedResults.Problem(
                            detail: "Organization not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }
                    return TypedResults.Ok(organization);
                }
            )
            .WithName("GetOrganizationById");

        group
            .MapGet(
                "/by-slug/{slug}",
                async Task<Results<Ok<OrganizationDtoForDetail>, ProblemHttpResult>> (
                    string slug,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await repository.GetBySlugAsync(slug, ct);
                    if (organization is null)
                    {
                        return TypedResults.Problem(
                            detail: "Organization not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }
                    return TypedResults.Ok(organization);
                }
            )
            .WithName("GetOrganizationBySlug")
            .AllowAnonymous();

        group
            .MapPost(
                "/",
                async Task<Results<Created<OrganizationDtoForCreated>, ProblemHttpResult>> (
                    OrganizationDtoForCreate dto,
                    IOrganizationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var exists = await repository.ExistsAsync(dto.Name, ct);
                    if (exists)
                    {
                        return TypedResults.Problem(
                            detail: "An organization with this name already exists.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var created = await repository.CreateAsync(dto, ct);
                    return TypedResults.Created($"/organization/organizations/{created.Id}", created);
                }
            )
            .WithName("CreateOrganization")
            .RequireAuthorization(PolicyNames.Admin);

        group
            .MapPut(
                "/{id:guid}",
                async Task<
                    Results<Ok<OrganizationDtoForDetail>, ProblemHttpResult, ForbidHttpResult>
                > (
                    Guid id,
                    OrganizationDtoForUpdate dto,
                    ClaimsPrincipal user,
                    IOrganizationRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            id,
                            Permissions.Organization.Update,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var updated = await repository.UpdateAsync(id, dto, ct);
                    if (updated is null)
                    {
                        return TypedResults.Problem(
                            detail: "Organization not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }
                    return TypedResults.Ok(updated);
                }
            )
            .WithName("UpdateOrganization")
            .RequireAuthorization();

        group
            .MapDelete(
                "/{id:guid}",
                async Task<Results<NoContent, ProblemHttpResult, ForbidHttpResult>> (
                    Guid id,
                    ClaimsPrincipal user,
                    IOrganizationRepository repository,
                    ISensorRepository sensorRepository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            id,
                            Permissions.Organization.Delete,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var sensorCount = await sensorRepository.GetCountByOrganizationAsync(id, ct);
                    if (sensorCount > 0)
                    {
                        return TypedResults.Problem(
                            detail: $"Cannot delete organization with {sensorCount} active sensor(s). Please delete or reassign all sensors first.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var deleted = await repository.DeleteAsync(id, ct);
                    if (!deleted)
                    {
                        return TypedResults.Problem(
                            detail: "Organization not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }
                    return TypedResults.NoContent();
                }
            )
            .WithName("DeleteOrganization")
            .RequireAuthorization();

        return app;
    }
}
