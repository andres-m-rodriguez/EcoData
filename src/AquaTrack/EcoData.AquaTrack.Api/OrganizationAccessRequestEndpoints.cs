using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.Contracts.Requests;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Database.Models;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.AquaTrack.Contracts.Permissions;

namespace EcoData.AquaTrack.Api;

public static class OrganizationAccessRequestEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationAccessRequestEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var orgGroup = app.MapGroup("/api/organizations/{organizationId:guid}/access-requests")
            .WithTags("Organization Access Requests")
            .RequireAuthorization();

        orgGroup
            .MapPost(
                "/",
                async Task<
                    Results<
                        Created<OrganizationAccessRequestDto>,
                        Conflict<string>,
                        UnauthorizedHttpResult
                    >
                > (
                    Guid organizationId,
                    CreateOrganizationAccessRequestRequest request,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository accessRequestRepository,
                    IOrganizationMemberRepository memberRepository,
                    IOrganizationBlockedUserRepository blockedUserRepository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var isBlocked = await blockedUserRepository.IsBlockedAsync(
                        organizationId,
                        token.UserId.Value,
                        ct
                    );
                    if (isBlocked)
                        return TypedResults.Conflict(
                            "You are blocked from requesting access to this organization."
                        );

                    var isMember = await memberRepository.ExistsAsync(
                        organizationId,
                        token.UserId.Value,
                        ct
                    );
                    if (isMember)
                    {
                        return TypedResults.Conflict(
                            "You are already a member of this organization."
                        );
                    }

                    var hasPending = await accessRequestRepository.ExistsPendingAsync(
                        token.UserId.Value,
                        organizationId,
                        ct
                    );
                    if (hasPending)
                    {
                        return TypedResults.Conflict(
                            "You already have a pending access request for this organization."
                        );
                    }

                    var accessRequest = await accessRequestRepository.CreateAsync(
                        token.UserId.Value,
                        organizationId,
                        request.RequestMessage,
                        ct
                    );

                    return TypedResults.Created(
                        $"/api/organizations/{organizationId}/access-requests/{accessRequest.Id}",
                        accessRequest
                    );
                }
            )
            .WithName("CreateOrganizationAccessRequest");

        orgGroup
            .MapGet(
                "/",
                async (
                    Guid organizationId,
                    [AsParameters] OrganizationAccessRequestParameters parameters,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository accessRequestRepository,
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
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(
                        accessRequestRepository.GetByOrganizationAsync(
                            organizationId,
                            parameters,
                            ct
                        )
                    );
                }
            )
            .WithName("GetOrganizationAccessRequests");

        orgGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<OrganizationAccessRequestDto>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
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
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var accessRequest = await repository.GetByIdAsync(id, ct);
                    if (accessRequest is null || accessRequest.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(accessRequest);
                }
            )
            .WithName("GetOrganizationAccessRequest");

        orgGroup
            .MapPut(
                "/{id:guid}/status",
                async Task<
                    Results<
                        Ok<OrganizationAccessRequestDto>,
                        NotFound,
                        BadRequest<string>,
                        ForbidHttpResult
                    >
                > (
                    Guid organizationId,
                    Guid id,
                    UpdateOrganizationAccessRequestStatusRequest request,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
                    IOrganizationMemberRepository memberRepository,
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
                            Permissions.Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var existingRequest = await repository.GetByIdAsync(id, ct);
                    if (existingRequest is null || existingRequest.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    if (
                        existingRequest.Status != OrganizationAccessRequestStatus.Pending.ToString()
                    )
                    {
                        return TypedResults.BadRequest("This request has already been processed.");
                    }

                    var status = request.Approved
                        ? OrganizationAccessRequestStatus.Approved
                        : OrganizationAccessRequestStatus.Rejected;

                    var updatedRequest = await repository.UpdateStatusAsync(
                        id,
                        status,
                        request.ReviewNotes,
                        token.UserId.Value,
                        ct
                    );

                    if (updatedRequest is null)
                    {
                        return TypedResults.NotFound();
                    }

                    if (request.Approved)
                    {
                        await memberRepository.CreateAsync(
                            organizationId,
                            existingRequest.UserId,
                            "Viewer",
                            ct
                        );
                    }

                    return TypedResults.Ok(updatedRequest);
                }
            )
            .WithName("UpdateOrganizationAccessRequestStatus");

        var meGroup = app.MapGroup("/api/me/access-requests")
            .WithTags("My Access Requests")
            .RequireAuthorization();

        meGroup
            .MapGet(
                "/",
                (
                    [AsParameters] OrganizationAccessRequestParameters parameters,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok(
                        repository.GetByUserAsync(token.UserId.Value, parameters, ct)
                    );
                }
            )
            .WithName("GetMyAccessRequests");

        meGroup
            .MapDelete(
                "/{id:guid}",
                async Task<
                    Results<NoContent, NotFound, BadRequest<string>, UnauthorizedHttpResult>
                > (
                    Guid id,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(user);
                    if (!token.IsAuthenticated)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var accessRequest = await repository.GetByIdAsync(id, ct);
                    if (accessRequest is null || accessRequest.UserId != token.UserId.Value)
                    {
                        return TypedResults.NotFound();
                    }

                    if (accessRequest.Status != OrganizationAccessRequestStatus.Pending.ToString())
                    {
                        return TypedResults.BadRequest("Only pending requests can be cancelled.");
                    }

                    var deleted = await repository.DeleteAsync(id, ct);
                    return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("CancelMyAccessRequest");

        return app;
    }
}
