using System.Security.Claims;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Organization.Contracts.Permissions;

namespace EcoData.Organization.Api;

public static class OrganizationRoleEndpoints
{
    private const int MaxLength = 100;

    public static IEndpointRouteBuilder MapOrganizationRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organization/organizations/{organizationId:guid}/roles")
            .WithTags("Organization Roles")
            .RequireAuthorization();

        // Any signed-in user may read roles: a prospective member picks one when requesting
        // access, before holding any membership in the organization.
        group
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<OrganizationRoleDto>>> (
                    Guid organizationId,
                    IOrganizationRoleRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetByOrganizationAsync(organizationId, ct))
            )
            .WithName("GetOrganizationRoles");

        group
            .MapPost(
                "/",
                async Task<Results<Created<OrganizationRoleDto>, ProblemHttpResult, ForbidHttpResult>> (
                    Guid organizationId,
                    OrganizationRoleRequest request,
                    ClaimsPrincipal user,
                    IOrganizationRoleRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    if (!await CanManageAsync(user, organizationId, permissionService, ct))
                        return TypedResults.Forbid();

                    var invalid = Normalize(request, out var name, out var permissions);
                    if (invalid is not null)
                        return invalid;

                    if (await repository.NameExistsAsync(organizationId, name, null, ct))
                        return TypedResults.Problem(
                            detail: $"A role named '{name}' already exists in this organization.",
                            statusCode: StatusCodes.Status409Conflict
                        );

                    var role = await repository.CreateAsync(organizationId, name, permissions, ct);

                    return TypedResults.Created(
                        $"/organization/organizations/{organizationId}/roles/{role.Id}",
                        role
                    );
                }
            )
            .WithName("CreateOrganizationRole");

        group
            .MapPut(
                "/{roleId:guid}",
                async Task<Results<Ok<OrganizationRoleDto>, ProblemHttpResult, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid roleId,
                    OrganizationRoleRequest request,
                    ClaimsPrincipal user,
                    IOrganizationRoleRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    if (!await CanManageAsync(user, organizationId, permissionService, ct))
                        return TypedResults.Forbid();

                    var invalid = Normalize(request, out var name, out var permissions);
                    if (invalid is not null)
                        return invalid;

                    if (await repository.NameExistsAsync(organizationId, name, roleId, ct))
                        return TypedResults.Problem(
                            detail: $"A role named '{name}' already exists in this organization.",
                            statusCode: StatusCodes.Status409Conflict
                        );

                    var role = await repository.UpdateAsync(
                        organizationId,
                        roleId,
                        name,
                        permissions,
                        ct
                    );

                    if (role is null)
                        return TypedResults.Problem(
                            detail: "Role not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );

                    return TypedResults.Ok(role);
                }
            )
            .WithName("UpdateOrganizationRole");

        group
            .MapDelete(
                "/{roleId:guid}",
                async Task<Results<NoContent, ProblemHttpResult, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid roleId,
                    ClaimsPrincipal user,
                    IOrganizationRoleRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    if (!await CanManageAsync(user, organizationId, permissionService, ct))
                        return TypedResults.Forbid();

                    var role = await repository.GetByIdAsync(organizationId, roleId, ct);
                    if (role is null)
                        return TypedResults.Problem(
                            detail: "Role not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );

                    if (await repository.IsInUseAsync(roleId, ct))
                        return TypedResults.Problem(
                            detail: $"'{role.Name}' is still assigned to members or pending access requests. Move them to another role first.",
                            statusCode: StatusCodes.Status409Conflict
                        );

                    await repository.DeleteAsync(organizationId, roleId, ct);

                    return TypedResults.NoContent();
                }
            )
            .WithName("DeleteOrganizationRole");

        return app;
    }

    // Roles decide what members may do, so changing them is part of managing members.
    private static async Task<bool> CanManageAsync(
        ClaimsPrincipal user,
        Guid organizationId,
        IOrganizationPermissionService permissionService,
        CancellationToken ct
    )
    {
        var token = new RequestClaimToken(user);

        if (!token.IsAuthenticated)
            return false;

        return await permissionService.HasPermissionAsync(
            token.UserId.Value,
            organizationId,
            Permissions.Organization.ManageMembers,
            ct
        );
    }

    // Permission keys are opaque strings here: the modules that own them (Sensors, Wildlife,
    // Organization itself) declare them, and the client offers the catalogue.
    private static ProblemHttpResult? Normalize(
        OrganizationRoleRequest request,
        out string name,
        out IReadOnlyList<string> permissions
    )
    {
        name = request.Name?.Trim() ?? "";
        permissions = (request.Permissions ?? [])
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (name.Length == 0 || name.Length > MaxLength)
            return TypedResults.Problem(
                detail: $"Role name must be between 1 and {MaxLength} characters.",
                statusCode: StatusCodes.Status400BadRequest
            );

        if (permissions.Any(p => p.Length > MaxLength))
            return TypedResults.Problem(
                detail: $"Permission keys must be at most {MaxLength} characters.",
                statusCode: StatusCodes.Status400BadRequest
            );

        return null;
    }
}
