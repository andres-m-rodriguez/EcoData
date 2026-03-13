using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.DataAccess.Interfaces;

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
