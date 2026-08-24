using EcoData.Common.Authorization;
using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.Application.Client;

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
        if (!_cache.TryGetValue(organizationId, out var task))
        {
            task = FetchPermissionsAsync(organizationId);
            _cache[organizationId] = task;
        }

        var permissions = await task.WaitAsync(cancellationToken);

        if (permissions.IsGlobalAdmin)
            return true;

        return permissions.Permissions.Contains(permission.Key);
    }

    public async Task<OrganizationGrants> GrantsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (!_cache.TryGetValue(organizationId, out var task))
        {
            task = FetchPermissionsAsync(organizationId);
            _cache[organizationId] = task;
        }

        var permissions = await task.WaitAsync(cancellationToken);

        return new OrganizationGrants(
            permissions.Permissions.ToHashSet(StringComparer.Ordinal),
            permissions.IsGlobalAdmin
        );
    }

    private async Task<UserPermissionsDto> FetchPermissionsAsync(Guid organizationId)
    {
        try
        {
            var result = await permissionClient.GetMyPermissionsAsync(
                organizationId,
                CancellationToken.None
            );

            return result.Match(
                permissions => permissions,
                _ =>
                {
                    _cache.Remove(organizationId);
                    return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false);
                }
            );
        }
        catch
        {
            _cache.Remove(organizationId);
            throw;
        }
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
