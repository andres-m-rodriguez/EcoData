using EcoData.Organization.Application.Client;
using EcoData.Organization.Contracts;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Parameters;
using EcoPortal.Client.Services;

namespace EcoPortal.Client.Features.Organizations.Workspace;

// One fetch per organization per session for the things every section needs:
// the organization, the caller's grants and role, and the rail counts. Moving
// between sections costs nothing; an auth change or a save drops the cache.
public sealed class OrganizationWorkspaceState(
    IOrganizationHttpClient organizationClient,
    IPermissionHttpClient permissionClient,
    IOrganizationMemberHttpClient memberClient,
    IOrganizationAccessRequestHttpClient accessRequestClient,
    ISensorHttpClient sensorClient,
    AuthStateService authState
)
{
    private const int PendingRequestsCap = 20;

    private readonly Dictionary<Guid, Task<OrganizationContext?>> _contexts = [];
    private readonly Dictionary<Guid, Task<OrganizationCounts>> _counts = [];

    // Null means the organization does not exist (or could not be reached).
    public Task<OrganizationContext?> GetAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (_contexts.TryGetValue(organizationId, out var cached))
        {
            return cached;
        }

        var task = LoadContextAsync(organizationId);
        _contexts[organizationId] = task;

        return task.WaitAsync(cancellationToken);
    }

    // A faulted load must not stick: the next visit retries instead of awaiting
    // the same dead task until something calls Invalidate.
    private async Task<OrganizationContext?> LoadContextAsync(Guid organizationId)
    {
        try
        {
            return await LoadContextCoreAsync(organizationId);
        }
        catch
        {
            _contexts.Remove(organizationId);
            throw;
        }
    }

    public Task<OrganizationCounts> GetCountsAsync(
        OrganizationContext context,
        CancellationToken cancellationToken = default
    )
    {
        var id = context.Organization.Id;

        if (_counts.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var task = LoadCountsAsync(context);
        _counts[id] = task;

        return task.WaitAsync(cancellationToken);
    }

    public void Invalidate(Guid? organizationId = null)
    {
        if (organizationId is { } id)
        {
            _contexts.Remove(id);
            _counts.Remove(id);
            return;
        }

        _contexts.Clear();
        _counts.Clear();
    }

    // The shared fetches never carry a caller's token: a section cancelling its
    // own load must not fault the task cached for the next one.
    private async Task<OrganizationContext?> LoadContextCoreAsync(Guid organizationId)
    {
        var organization = await organizationClient.GetByIdAsync(organizationId);
        if (!organization.TryPickT0(out var dto, out _))
        {
            _contexts.Remove(organizationId);
            return null;
        }

        var permissions = await permissionClient.GetMyPermissionsAsync(organizationId);
        var grants = permissions.Match(
            p => (Permissions: (IReadOnlySet<string>)p.Permissions.ToHashSet(StringComparer.Ordinal), p.IsGlobalAdmin),
            _ => (Permissions: (IReadOnlySet<string>)new HashSet<string>(), IsGlobalAdmin: false)
        );

        string? roleName = null;
        if (authState.IsAuthenticated)
        {
            await foreach (var mine in organizationClient.GetMyOrganizationsAsync())
            {
                if (mine.Id == organizationId)
                {
                    roleName = mine.RoleName;
                    break;
                }
            }
        }

        return new OrganizationContext(dto, grants.Permissions, grants.IsGlobalAdmin, roleName);
    }

    private async Task<OrganizationCounts> LoadCountsAsync(OrganizationContext context)
    {
        var id = context.Organization.Id;

        var sensors = await sensorClient.GetSensorCountAsync(new SensorParameters(OrganizationId: id));
        var sensorCount = sensors.Match(count => (int?)count, _ => null);

        int? memberCount = null;
        int? pending = null;

        if (context.Can(Permissions.Organization.ManageMembers))
        {
            memberCount = 0;
            await foreach (var _ in memberClient.GetAllAsync(id, new OrganizationMemberParameters(PageSize: 200)))
            {
                memberCount++;
            }

            pending = 0;
            await foreach (var _ in accessRequestClient.GetByOrganizationAsync(
                id,
                new OrganizationAccessRequestParameters(PageSize: PendingRequestsCap, Status: "Pending")
            ))
            {
                pending++;
            }
        }

        return new OrganizationCounts(sensorCount, memberCount, pending);
    }
}
