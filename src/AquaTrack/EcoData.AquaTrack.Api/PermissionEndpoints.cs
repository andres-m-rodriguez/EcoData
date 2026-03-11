using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Claims;
using EcoData.Identity.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/my-permissions")
            .WithTags("Permissions")
            .RequireAuthorization();

        group.MapGet(
            "",
            async Task<Results<Ok<UserPermissionsDto>, UnauthorizedHttpResult>> (
                Guid organizationId,
                ClaimsPrincipal user,
                IOrganizationMembershipRepository membershipRepository,
                IUserLookupRepository userLookupRepository,
                CancellationToken ct
            ) =>
            {
                var token = new RequestClaimToken(user);
                if (!token.IsAuthenticated)
                {
                    return TypedResults.Unauthorized();
                }

                var userId = token.UserId!.Value;
                var isGlobalAdmin = await userLookupRepository.IsGlobalAdminAsync(userId, ct);

                if (isGlobalAdmin)
                {
                    return TypedResults.Ok(new UserPermissionsDto(
                        organizationId,
                        [],
                        true
                    ));
                }

                var membership = await membershipRepository.GetAsync(userId, organizationId, ct);

                return TypedResults.Ok(new UserPermissionsDto(
                    organizationId,
                    membership?.Permissions ?? [],
                    false
                ));
            }
        ).WithName("GetMyPermissions");

        return app;
    }
}
