using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts.Dtos;

namespace EcoPortal.Client.Services;

public sealed class PermissionContextService(
    IPermissionHttpClient permissionClient,
    AuthStateService authState
)
{
    private readonly Dictionary<Guid, Task<UserPermissionsDto>> _cache = [];

    public async Task<bool> HasPermissionAsync(
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        if (authState.IsGlobalAdmin)
        {
            return true;
        }

        var permissions = await GetPermissionsAsync(organizationId, cancellationToken);

        if (permissions.IsGlobalAdmin)
        {
            return true;
        }

        return permissions.Permissions.Contains(permission);
    }

    public Task<UserPermissionsDto> GetPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (_cache.TryGetValue(organizationId, out var cachedTask))
        {
            return cachedTask;
        }

        var task = permissionClient.GetMyPermissionsAsync(organizationId, cancellationToken);
        _cache[organizationId] = task;

        return task;
    }

    public void InvalidateCache(Guid? organizationId = null)
    {
        if (organizationId.HasValue)
        {
            _cache.Remove(organizationId.Value);
        }
        else
        {
            _cache.Clear();
        }
    }
}
