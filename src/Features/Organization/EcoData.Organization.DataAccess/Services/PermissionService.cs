using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Identity.DataAccess.Interfaces;

namespace EcoData.Organization.DataAccess.Services;

public sealed class PermissionService(
    IOrganizationMembershipRepository membershipRepository,
    IUserLookupRepository userLookupRepository
) : IPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        // GlobalAdmin has all permissions
        if (await userLookupRepository.IsGlobalAdminAsync(userId, cancellationToken))
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
}
