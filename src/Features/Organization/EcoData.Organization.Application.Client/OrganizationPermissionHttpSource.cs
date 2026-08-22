using EcoData.Common.Authorization;
using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.Application.Client;

// Client half of the client/server source pair — same contract as the server's
// storage-backed source, answered from a per-organization cached HTTP fetch.
// Client answers only shape UI; the endpoint check on the server is the enforcement.
public sealed class OrganizationPermissionHttpSource(IPermissionHttpClient permissionClient)
    : IOrganizationPermissionSource
{
    private readonly Dictionary<Guid, Task<UserPermissionsDto>> _cache = [];

    public async Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await GetPermissionsAsync(organizationId, cancellationToken);

        if (permissions.IsGlobalAdmin)
        {
            return true;
        }

        return permissions.Permissions.Contains(permission.Key);
    }

    private Task<UserPermissionsDto> GetPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        if (_cache.TryGetValue(organizationId, out var cachedTask))
        {
            return cachedTask;
        }

        var task = FetchPermissionsAsync(organizationId, cancellationToken);
        _cache[organizationId] = task;

        return task;
    }

    private async Task<UserPermissionsDto> FetchPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var result = await permissionClient.GetMyPermissionsAsync(organizationId, cancellationToken);

        return result.Match(
            permissions => permissions,
            _ =>
            {
                // Don't cache failures — a later call should be able to retry.
                _cache.Remove(organizationId);
                return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false);
            }
        );
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
