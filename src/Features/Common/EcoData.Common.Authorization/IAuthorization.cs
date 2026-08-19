namespace EcoData.Common.Authorization;

/// <summary>
/// The one thing callers use to ask an authorization question, whatever the answer is
/// backed by. Always about the current user.
/// </summary>
/// <remarks>
/// Ambient — there is no principal parameter — because the browser has no
/// <c>ClaimsPrincipal</c> to pass, and the same call has to compile on both sides. Server
/// code asking about somebody other than the caller should go to the owning module directly.
/// <para>
/// Both questions take a scope, so "Owner of this organization" and "GlobalAdmin anywhere"
/// are the same shape:
/// <code>
/// await auth.HasPermissionAsync(PermissionScope.Organization(orgId), "sensor:update");
/// await auth.HasPermissionAsync(PermissionScope.Application("faunafinder"), "fauna:occurrence:submit");
/// await auth.IsInRoleAsync(PermissionScope.Global, "GlobalAdmin");
/// </code>
/// </para>
/// </remarks>
public interface IAuthorization
{
    Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    );
}
