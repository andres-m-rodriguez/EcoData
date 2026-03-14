using System.Security.Claims;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Organization.Contracts.Permissions;

namespace EcoData.Organization.Api;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/members")
            .WithTags("Organization Members")
            .RequireAuthorization();

        group
            .MapGet(
                "/",
                (
                    Guid organizationId,
                    [AsParameters] OrganizationMemberParameters parameters,
                    IOrganizationMemberRepository repository,
                    CancellationToken ct
                ) => repository.GetAllAsync(organizationId, parameters, ct)
            )
            .WithName("GetOrganizationMembers")
            .AllowAnonymous();

        group
            .MapGet(
                "/{userId:guid}",
                async Task<Results<Ok<OrganizationMemberDto>, NotFound>> (
                    Guid organizationId,
                    Guid userId,
                    IOrganizationMemberRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var member = await repository.GetAsync(organizationId, userId, ct);
                    return member is null ? TypedResults.NotFound() : TypedResults.Ok(member);
                }
            )
            .WithName("GetOrganizationMember");

        group
            .MapPut(
                "/{userId:guid}",
                async Task<Results<Ok<OrganizationMemberDto>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid userId,
                    UpdateMemberRoleRequest request,
                    ClaimsPrincipal user,
                    IOrganizationMemberRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var member = await repository.UpdateAsync(
                        organizationId,
                        userId,
                        request.Role,
                        ct
                    );
                    if (member is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(member);
                }
            )
            .WithName("UpdateOrganizationMember");

        group
            .MapDelete(
                "/{userId:guid}",
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid userId,
                    ClaimsPrincipal user,
                    IOrganizationMemberRepository repository,
                    IOrganizationPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var deleted = await repository.DeleteAsync(organizationId, userId, ct);
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("RemoveOrganizationMember");

        return app;
    }
}
