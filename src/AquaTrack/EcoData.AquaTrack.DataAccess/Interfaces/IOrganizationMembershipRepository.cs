using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationMembershipRepository
{
    Task<IReadOnlyList<OrganizationMembershipDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMembershipDto?> GetAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
