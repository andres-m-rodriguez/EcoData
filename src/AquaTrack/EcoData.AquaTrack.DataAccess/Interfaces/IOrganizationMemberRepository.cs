using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationMemberRepository
{
    Task<IReadOnlyList<OrganizationMembershipDto>> GetAllOrganizationMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMembershipDto?> GetOrganizationMembershipAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
