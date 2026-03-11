using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Claims;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using static EcoData.AquaTrack.Contracts.Permissions;

namespace EcoData.AquaTrack.Api;

// TODO: When migrating to microservices, replace IUserLookupRepository with HTTP/gRPC client.
// This interface currently lives in Identity.DataAccess which won't be accessible from AquaTrack.
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
                    IOrganizationMemberRepository repository,
                    CancellationToken ct
                ) => repository.GetAllAsync(organizationId, ct)
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
            .MapPost(
                "/",
                async Task<
                    Results<
                        Created<OrganizationMemberDto>,
                        NotFound,
                        Conflict<string>,
                        ForbidHttpResult
                    >
                > (
                    Guid organizationId,
                    AddMemberRequest request,
                    ClaimsPrincipal user,
                    IOrganizationMemberRepository repository,
                    IPermissionService permissionService,
                    IUserLookupRepository userLookupRepository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var userExists = await userLookupRepository.ExistsAsync(request.UserId, ct);
                    if (!userExists)
                    {
                        return TypedResults.NotFound();
                    }

                    var exists = await repository.ExistsAsync(organizationId, request.UserId, ct);
                    if (exists)
                    {
                        return TypedResults.Conflict(
                            "User is already a member of this organization."
                        );
                    }

                    var member = await repository.CreateAsync(
                        organizationId,
                        request.UserId,
                        request.Role,
                        ct
                    );
                    if (member is null)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Created(
                        $"/api/organizations/{organizationId}/members/{member.UserId}",
                        member
                    );
                }
            )
            .WithName("AddOrganizationMember");

        group
            .MapPut(
                "/{userId:guid}",
                async Task<Results<Ok<OrganizationMemberDto>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid userId,
                    UpdateMemberRoleRequest request,
                    ClaimsPrincipal user,
                    IOrganizationMemberRepository repository,
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Organization.ManageMembers,
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
                    IPermissionService permissionService,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (
                        !token.IsAuthenticated
                        || !await permissionService.HasPermissionAsync(
                            token.UserId.Value,
                            organizationId,
                            Organization.ManageMembers,
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
