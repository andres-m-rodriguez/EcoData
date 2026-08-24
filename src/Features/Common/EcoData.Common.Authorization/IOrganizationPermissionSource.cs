namespace EcoData.Common.Authorization;

// Implemented only by the module that owns organization membership storage. Features
// never implement sources — they ask IAuthorization and stay unaware of the answer's origin.
public interface IOrganizationPermissionSource
{
    Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<OrganizationGrants> GrantsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
