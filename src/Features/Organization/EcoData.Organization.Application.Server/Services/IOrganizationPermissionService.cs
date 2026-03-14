namespace EcoData.Organization.Application.Server.Services;

public interface IOrganizationPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    );
}
