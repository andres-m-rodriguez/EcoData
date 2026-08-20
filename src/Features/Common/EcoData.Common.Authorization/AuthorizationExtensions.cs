namespace EcoData.Common.Authorization;

// Call-site shorthand for the scope kinds this library defines. Extensions rather than
// interface members: implementations stay at two methods, and a new scope kind adds an
// extension here instead of breaking every implementation.
public static class AuthorizationExtensions
{
    public static Task<bool> HasOrgPermissionAsync(
        this IAuthorization authorization,
        Guid organizationId,
        string permission,
        CancellationToken cancellationToken = default
    ) =>
        authorization.HasPermissionAsync(
            PermissionScope.Organization(organizationId),
            permission,
            cancellationToken
        );

    public static Task<bool> IsInOrgRoleAsync(
        this IAuthorization authorization,
        Guid organizationId,
        string role,
        CancellationToken cancellationToken = default
    ) =>
        authorization.IsInRoleAsync(
            PermissionScope.Organization(organizationId),
            role,
            cancellationToken
        );

    public static Task<bool> IsInGlobalRoleAsync(
        this IAuthorization authorization,
        string role,
        CancellationToken cancellationToken = default
    ) => authorization.IsInRoleAsync(PermissionScope.Global, role, cancellationToken);
}
