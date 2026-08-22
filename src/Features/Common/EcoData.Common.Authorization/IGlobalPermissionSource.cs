namespace EcoData.Common.Authorization;

// Implemented only by the module that owns global roles.
public interface IGlobalPermissionSource
{
    Task<bool> HasAsync(
        IGlobalPermission permission,
        CancellationToken cancellationToken = default
    );
}
