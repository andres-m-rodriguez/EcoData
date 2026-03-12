using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts.Dtos;

namespace EcoPortal.Client.Services;

public sealed class PermissionContextService(
    IPermissionHttpClient permissionClient,
    AuthStateService authState
)
{
    private readonly Dictionary<Guid, UserPermissionsDto> _cache = [];

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

    public async Task<UserPermissionsDto> GetPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (_cache.TryGetValue(organizationId, out var cached))
        {
            return cached;
        }

        var permissions = await permissionClient.GetMyPermissionsAsync(
            organizationId,
            cancellationToken
        );
        _cache[organizationId] = permissions;

        return permissions;
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
