using EcoData.AquaTrack.DataAccess.Interfaces;

namespace EcoData.AquaTrack.DataAccess.Services;

public sealed class PermissionService(IOrganizationMembershipRepository membershipRepository)
    : IPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        var membership = await membershipRepository.GetAsync(userId, organizationId, cancellationToken);

        if (membership is null)
        {
            return false;
        }

        return membership.Permissions.Contains(permission);
    }
}
