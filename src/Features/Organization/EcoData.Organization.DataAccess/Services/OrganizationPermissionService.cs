using EcoData.Identity.Application.Server.Services;
using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;

namespace EcoData.Organization.DataAccess.Services;

public sealed class OrganizationPermissionService(
    IOrganizationMembershipRepository membershipRepository,
    IUserLookupService userLookupService
) : IOrganizationPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        if (await userLookupService.IsGlobalAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        var membership = await membershipRepository.GetAsync(
            userId,
            organizationId,
            cancellationToken
        );

        if (membership is null)
        {
            return false;
        }

        return membership.Permissions.Contains(permission);
    }

    public async Task<UserPermissionsDto> GetPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (await userLookupService.IsGlobalAdminAsync(userId, cancellationToken))
        {
            return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: true);
        }

        var membership = await membershipRepository.GetAsync(
            userId,
            organizationId,
            cancellationToken
        );

        return new UserPermissionsDto(
            organizationId,
            membership?.Permissions ?? [],
            IsGlobalAdmin: false
        );
    }
}
