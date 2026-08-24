namespace EcoData.Common.Authorization;

// Sources are optional so hosts register only the scope types they use, but a check
// against a missing source is a wiring mistake, not a denied user. Failing loudly beats
// returning false, which would look exactly like "correctly configured, no access".
public sealed class Authorization(
    IOrganizationPermissionSource? organizationSource = null,
    IGlobalPermissionSource? globalSource = null
) : IAuthorization
{
    public Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (organizationSource is null)
            throw MissingSource(nameof(IOrganizationPermissionSource), permission.Key);

        return organizationSource.HasAsync(permission, organizationId, cancellationToken);
    }

    public Task<bool> HasAsync(
        IGlobalPermission permission,
        CancellationToken cancellationToken = default
    )
    {
        if (globalSource is null)
            throw MissingSource(nameof(IGlobalPermissionSource), permission.Key);

        return globalSource.HasAsync(permission, cancellationToken);
    }

    public Task<OrganizationGrants> GrantsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (organizationSource is null)
            throw MissingSource(nameof(IOrganizationPermissionSource), "organization grants");

        return organizationSource.GrantsAsync(organizationId, cancellationToken);
    }

    private static InvalidOperationException MissingSource(string source, string key) =>
        new(
            $"No {source} is registered, so '{key}' cannot be answered. "
                + "Register one in the host before checking this permission."
        );
}
