using System.Security.Claims;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Organization.Api;

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
                IUserLookupService userLookupService,
                CancellationToken ct
            ) =>
            {
                var token = new RequestClaimToken(user);
                if (!token.IsAuthenticated)
                {
                    return TypedResults.Unauthorized();
                }

                var userId = token.UserId!.Value;
                var isGlobalAdmin = await userLookupService.IsGlobalAdminAsync(userId, ct);

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
