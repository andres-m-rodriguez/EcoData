using EcoData.Common.Authorization;
using EcoData.Identity.Contracts.Claims;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EcoData.Organization.Authorization;

/// <summary>
/// Answers <see cref="PermissionScope.OrganizationKind"/> questions from the caller's
/// membership. Registered by any host that asks organization-scoped questions.
/// </summary>
/// <remarks>
/// Organization owns roles and their permissions, so this is the only place that reads
/// them. Feature modules ask <see cref="IAuthorization"/> and never see this type.
/// </remarks>
public sealed class OrganizationPermissionSource(
    IHttpContextAccessor httpContextAccessor,
    IOrganizationPermissionService permissions,
    IOrganizationMembershipRepository memberships
) : IPermissionSource
{
    public string ScopeKind => PermissionScope.OrganizationKind;

    public async Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        // GlobalAdmin short-circuits inside the permission service, so a global admin is
        // allowed here without the global source being registered.
        if (!TryResolve(scope, out var userId, out var organizationId))
            return false;

        return await permissions.HasPermissionAsync(
            userId,
            organizationId,
            permission,
            cancellationToken
        );
    }

    public async Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryResolve(scope, out var userId, out var organizationId))
            return false;

        var membership = await memberships.GetAsync(userId, organizationId, cancellationToken);

        return membership is not null
            && string.Equals(membership.RoleName, role, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryResolve(PermissionScope scope, out Guid userId, out Guid organizationId)
    {
        userId = default;

        if (!Guid.TryParse(scope.Id, out organizationId))
            return false;

        var user = httpContextAccessor.HttpContext?.User;

        if (user is null)
            return false;

        var token = new RequestClaimToken(user);

        if (!token.IsAuthenticated)
            return false;

        userId = token.UserId.Value;

        return true;
    }
}
