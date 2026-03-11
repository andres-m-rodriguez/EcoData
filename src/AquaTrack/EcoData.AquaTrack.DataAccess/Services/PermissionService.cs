using EcoData.AquaTrack.DataAccess.Interfaces;

namespace EcoData.AquaTrack.DataAccess.Services;

public sealed class PermissionService(IOrganizationMemberRepository memberRepository)
    : IPermissionService
{
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        var membership = await memberRepository.GetOrganizationMembershipAsync(
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
