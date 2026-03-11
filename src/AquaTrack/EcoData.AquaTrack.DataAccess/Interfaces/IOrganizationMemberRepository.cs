using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IOrganizationMemberRepository
{
    IAsyncEnumerable<OrganizationMemberDto> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMemberDto?> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMemberDto?> CreateAsync(
        Guid organizationId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationMemberDto?> UpdateAsync(
        Guid organizationId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
