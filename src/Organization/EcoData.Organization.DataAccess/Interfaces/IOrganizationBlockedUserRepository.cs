using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.DataAccess.Interfaces;

public interface IOrganizationBlockedUserRepository
{
    IAsyncEnumerable<OrganizationBlockedUserDto> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsBlockedAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationBlockedUserDto> BlockAsync(
        Guid organizationId,
        Guid userId,
        Guid blockedByUserId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<bool> UnblockAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
