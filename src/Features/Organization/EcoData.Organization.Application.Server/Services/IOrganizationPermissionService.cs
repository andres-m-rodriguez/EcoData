using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.Application.Server.Services;

public interface IOrganizationPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    );

    Task<UserPermissionsDto> GetPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
