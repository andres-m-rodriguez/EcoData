using System.Security.Claims;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Application.Services;
using EcoData.Identity.Contracts.Claims;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using static EcoData.AquaTrack.Contracts.Permissions;

namespace EcoData.AquaTrack.Api;

// TODO: When migrating to microservices, replace IAccessRequestRepository with HTTP/gRPC client.
// This interface currently lives in Identity.DataAccess which won't be accessible from AquaTrack.
public static class AccessRequestEndpoints
{
    public static IEndpointRouteBuilder MapAccessRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/members/access-requests")
            .WithTags("Access Requests")
            .RequireAuthorization();

        group
            .MapGet(
                "/",
                async Task<Results<Ok<IAsyncEnumerable<AccessRequestResponse>>, ForbidHttpResult>> (
                    Guid organizationId,
                    [AsParameters] AccessRequestParameters parameters,
                    ClaimsPrincipal user,
                    IAccessRequestRepository accessRequestRepository,
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

                    return TypedResults.Ok(
                        accessRequestRepository.GetAccessRequestsForOrganizationAsync(
                            organizationId,
                            parameters,
                            ct
                        )
                    );
                }
            )
            .WithName("GetOrganizationAccessRequests");

        group
            .MapPut(
                "/{accessRequestId:guid}/status",
                async Task<
                    Results<Ok<AccessRequestResponse>, NotFound, Conflict<string>, ForbidHttpResult>
                > (
                    Guid organizationId,
                    Guid accessRequestId,
                    UpdateAccessRequestStatusRequest request,
                    ClaimsPrincipal user,
                    IAccessRequestRepository accessRequestRepository,
                    IAuthService authService,
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
                            Organization.ManageMembers,
                            ct
                        )
                    )
                    {
                        return TypedResults.Forbid();
                    }

                    var accessRequest = await accessRequestRepository.GetByIdAsync(
                        accessRequestId,
                        ct
                    );
                    if (
                        accessRequest is null
                        || accessRequest.RequestedOrganizationId != organizationId
                    )
                    {
                        return TypedResults.NotFound();
                    }

                    var result = await authService.UpdateAccessRequestStatusAsync(
                        accessRequestId,
                        request,
                        user,
                        ct
                    );

                    if (result.IsT1)
                        return TypedResults.NotFound();
                    if (result.IsT2)
                        return TypedResults.Conflict(
                            "This access request has already been processed"
                        );

                    var response = result.AsT0;

                    if (request.Approved && response.CreatedUserId.HasValue)
                    {
                        await memberRepository.CreateAsync(
                            organizationId,
                            response.CreatedUserId.Value,
                            "Viewer",
                            ct
                        );
                    }

                    return TypedResults.Ok(response);
                }
            )
            .WithName("UpdateAccessRequestStatus");

        return app;
    }
}
