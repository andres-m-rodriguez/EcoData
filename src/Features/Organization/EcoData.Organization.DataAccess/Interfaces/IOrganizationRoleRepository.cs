using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.DataAccess.Interfaces;

public interface IOrganizationRoleRepository
{
    Task<IReadOnlyList<OrganizationRoleDto>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationRoleDto?> GetByIdAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationRoleDto?> GetByNameAsync(
        Guid organizationId,
        string name,
        CancellationToken cancellationToken = default
    );

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludeRoleId = null,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationRoleDto> CreateAsync(
        Guid organizationId,
        string name,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationRoleDto?> UpdateAsync(
        Guid organizationId,
        Guid roleId,
        string name,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken = default
    );

    // True while any member holds the role or any pending access request asks for it.
    Task<bool> IsInUseAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    );
}
