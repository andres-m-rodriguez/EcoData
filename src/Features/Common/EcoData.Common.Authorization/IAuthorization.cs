namespace EcoData.Common.Authorization;


public interface IAuthorization
{
    Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    );
}
