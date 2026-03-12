using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.AquaTrack.Contracts.Permissions;

namespace EcoData.AquaTrack.Api;

public static class OrganizationBlockedUserEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationBlockedUserEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/blocked-users")
            .WithTags("Organization Blocked Users")
            .RequireAuthorization();

        group
            .MapGet(
                "/",
                async (
                    Guid organizationId,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId!.Value,
                            organizationId,
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(repository.GetByOrganizationAsync(organizationId, ct));
                }
            )
            .WithName("GetBlockedUsers");

        group
            .MapPost(
                "/",
                async Task<
                    Results<Created<OrganizationBlockedUserDto>, Conflict<string>, ForbidHttpResult>
                > (
                    Guid organizationId,
                    BlockUserRequest request,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId!.Value,
                            organizationId,
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var isAlreadyBlocked = await repository.IsBlockedAsync(
                        organizationId,
                        request.UserId,
                        ct
                    );
                    if (isAlreadyBlocked)
                    {
                        return TypedResults.Conflict("This user is already blocked.");
                    }

                    var blocked = await repository.BlockAsync(
                        organizationId,
                        request.UserId,
                        token.UserId!.Value,
                        request.Reason,
                        ct
                    );

                    return TypedResults.Created(
                        $"/api/organizations/{organizationId}/blocked-users/{request.UserId}",
                        blocked
                    );
                }
            )
            .WithName("BlockUser");

        group
            .MapDelete(
                "/{userId:guid}",
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid userId,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId!.Value,
                            organizationId,
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var unblocked = await repository.UnblockAsync(organizationId, userId, ct);
                    return unblocked ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("UnblockUser");

        return app;
    }
}
