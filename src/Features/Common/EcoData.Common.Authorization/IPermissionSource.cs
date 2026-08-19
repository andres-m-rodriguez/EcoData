namespace EcoData.Common.Authorization;

/// <summary>
/// Answers authorization questions for one kind of scope. Whichever module owns a scope's
/// storage implements this: Organization for <c>organization</c>, Identity for
/// <c>global</c>, and so on.
/// </summary>
/// <remarks>
/// Registering a source is how a scope kind becomes usable. Feature modules never implement
/// this — they ask <see cref="IAuthorization"/> and stay unaware of where answers come from.
/// </remarks>
public interface IPermissionSource
{
    /// <summary>
    /// The <see cref="PermissionScope.Kind"/> this source answers for. Exactly one source
    /// may claim a kind.
    /// </summary>
    string ScopeKind { get; }

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
