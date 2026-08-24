using EcoData.Common.Authorization;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using Microsoft.AspNetCore.Http;

namespace EcoData.Organization.Authorization;

public sealed class OrganizationPermissionSource(
    IHttpContextAccessor httpContextAccessor,
    IOrganizationPermissionService permissions
) : IOrganizationPermissionSource
{
    public async Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user is null)
            return false;

        var token = new RequestClaimToken(user);

        if (!token.IsAuthenticated)
            return false;

        return await permissions.HasPermissionAsync(
            token.UserId.Value,
            organizationId,
            permission.Key,
            cancellationToken
        );
    }

    public async Task<OrganizationGrants> GrantsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user is null)
            return OrganizationGrants.None;

        var token = new RequestClaimToken(user);

        if (!token.IsAuthenticated)
            return OrganizationGrants.None;

        var mine = await permissions.GetPermissionsAsync(
            token.UserId.Value,
            organizationId,
            cancellationToken
        );

        return new OrganizationGrants(
            mine.Permissions.ToHashSet(StringComparer.Ordinal),
            mine.IsGlobalAdmin
        );
    }
}
