namespace EcoData.Organization.DataAccess.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    );
}
