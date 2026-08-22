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
        // The fetch is shared by every caller asking about this organization, so it never
        // carries one caller's token: a component cancelling its own check (Tempest's
        // latest-wins re-execute) must not fault the task cached for everyone else.
        var permissions = await GetPermissionsAsync(organizationId).WaitAsync(cancellationToken);

        if (permissions.IsGlobalAdmin)
        {
            return true;
        }

        return permissions.Permissions.Contains(permission.Key);
    }

    private Task<UserPermissionsDto> GetPermissionsAsync(Guid organizationId)
    {
        if (_cache.TryGetValue(organizationId, out var cachedTask))
        {
            return cachedTask;
        }

        var task = FetchPermissionsAsync(organizationId);
        _cache[organizationId] = task;

        return task;
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
                    // Don't cache failures — a later call should be able to retry.
                    _cache.Remove(organizationId);
                    return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false);
                }
            );
        }
        catch
        {
            // Same rule for a fetch that throws: a faulted task must not be served from cache.
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
