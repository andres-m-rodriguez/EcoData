using System.Security.Claims;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Organization.Contracts.Permissions;

namespace EcoData.Organization.Api;

public static class OrganizationBlockedUserEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationBlockedUserEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var group = app.MapGroup("/organization/organizations/{organizationId:guid}/blocked-users")
            .WithTags("Organization Blocked Users")
            .RequireAuthorization();

        group
            .MapGet(
                "/",
                async (
                    Guid organizationId,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IOrganizationPermissionService permissionService,
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
                    Results<
                        Created<OrganizationBlockedUserDto>,
                        ProblemHttpResult,
                        ForbidHttpResult
                    >
                > (
                    Guid organizationId,
                    BlockUserRequest request,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IOrganizationPermissionService permissionService,
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
                        return TypedResults.Problem(
                            detail: "This user is already blocked.",
                            statusCode: StatusCodes.Status409Conflict
                        );
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
                async Task<Results<NoContent, ProblemHttpResult, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid userId,
                    ClaimsPrincipal user,
                    IOrganizationBlockedUserRepository repository,
                    IOrganizationPermissionService permissionService,
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
                    if (!unblocked)
                    {
                        return TypedResults.Problem(
                            detail: "User is not blocked.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }
                    return TypedResults.NoContent();
                }
            )
            .WithName("UnblockUser");

        return app;
    }
}
