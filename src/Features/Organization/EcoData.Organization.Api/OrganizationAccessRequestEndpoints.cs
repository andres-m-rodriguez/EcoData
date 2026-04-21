using System.Security.Claims;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.Contracts.Requests;
using EcoData.Organization.DataAccess.Interfaces;
using DefaultRoles = EcoData.Organization.Contracts.DefaultOrganizationRoles;
using EcoData.Organization.Database.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Permissions = EcoData.Organization.Contracts.Permissions;

namespace EcoData.Organization.Api;

public static class OrganizationAccessRequestEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationAccessRequestEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        var orgGroup = app.MapGroup("/organization/organizations/{organizationId:guid}/access-requests")
            .WithTags("Organization Access Requests")
            .RequireAuthorization();

        orgGroup
            .MapPost(
                "/",
                async Task<
                    Results<Created<OrganizationAccessRequestDto>, ProblemHttpResult, UnauthorizedHttpResult>
                > (
                    Guid organizationId,
                    CreateOrganizationAccessRequestRequest request,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
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
                    {
                        return TypedResults.Problem(
                            detail: "You are blocked from requesting access to this organization.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var isMember = await memberRepository.ExistsAsync(
                        organizationId,
                        token.UserId.Value,
                        ct
                    );
                    if (isMember)
                    {
                        return TypedResults.Problem(
                            detail: "You are already a member of this organization.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var hasPending = await repository.ExistsPendingAsync(
                        token.UserId.Value,
                        organizationId,
                        ct
                    );
                    if (hasPending)
                    {
                        return TypedResults.Problem(
                            detail: "You already have a pending access request for this organization.",
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    var accessRequest = await repository.CreateAsync(
                        token.UserId.Value,
                        organizationId,
                        request.RequestMessage,
                        ct
                    );

                    return TypedResults.Created(
                        $"/organization/organizations/{organizationId}/access-requests/{accessRequest.Id}",
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
                    IOrganizationAccessRequestRepository repository,
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
                        return Results.Forbid();
                    }

                    return Results.Ok(
                        repository.GetByOrganizationAsync(organizationId, parameters, ct)
                    );
                }
            )
            .WithName("GetOrganizationAccessRequests");

        orgGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<OrganizationAccessRequestDto>, ProblemHttpResult, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
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

                    var accessRequest = await repository.GetByIdAsync(id, ct);
                    if (accessRequest is null || accessRequest.OrganizationId != organizationId)
                    {
                        return TypedResults.Problem(
                            detail: "Access request not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    return TypedResults.Ok(accessRequest);
                }
            )
            .WithName("GetOrganizationAccessRequest");

        orgGroup
            .MapPut(
                "/{id:guid}/status",
                async Task<
                    Results<Ok<OrganizationAccessRequestDto>, ProblemHttpResult, ForbidHttpResult>
                > (
                    Guid organizationId,
                    Guid id,
                    UpdateOrganizationAccessRequestStatusRequest request,
                    ClaimsPrincipal user,
                    IOrganizationAccessRequestRepository repository,
                    IOrganizationMemberRepository memberRepository,
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

                    var existingRequest = await repository.GetByIdAsync(id, ct);
                    if (existingRequest is null || existingRequest.OrganizationId != organizationId)
                    {
                        return TypedResults.Problem(
                            detail: "Access request not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    if (
                        existingRequest.Status != OrganizationAccessRequestStatus.Pending.ToString()
                    )
                    {
                        return TypedResults.Problem(
                            detail: "This request has already been processed.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
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
                        return TypedResults.Problem(
                            detail: "Access request not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    if (request.Approved)
                    {
                        await memberRepository.CreateAsync(
                            organizationId,
                            existingRequest.UserId,
                            DefaultRoles.Viewer,
                            ct
                        );
                    }

                    return TypedResults.Ok(updatedRequest);
                }
            )
            .WithName("UpdateOrganizationAccessRequestStatus");

        var meGroup = app.MapGroup("/organization/me/access-requests")
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
            .MapPost(
                "/{id:guid}/cancel",
                async Task<
                    Results<Ok<OrganizationAccessRequestDto>, ProblemHttpResult, UnauthorizedHttpResult>
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
                        return TypedResults.Problem(
                            detail: "Access request not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    if (accessRequest.Status != OrganizationAccessRequestStatus.Pending.ToString())
                    {
                        return TypedResults.Problem(
                            detail: "Only pending requests can be cancelled.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }

                    var cancelled = await repository.CancelAsync(id, ct);
                    if (cancelled is null)
                    {
                        return TypedResults.Problem(
                            detail: "Access request not found.",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    return TypedResults.Ok(cancelled);
                }
            )
            .WithName("CancelMyAccessRequest");

        return app;
    }
}
