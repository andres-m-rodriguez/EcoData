using System.Collections.Frozen;

namespace EcoData.Common.Authorization;

/// <summary>
/// Routes each question to the source that owns its scope kind.
/// </summary>
public sealed class Authorization : IAuthorization
{
    private readonly FrozenDictionary<string, IPermissionSource> _sources;

    public Authorization(IEnumerable<IPermissionSource> sources)
    {
        var byKind = new Dictionary<string, IPermissionSource>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            if (!byKind.TryAdd(source.ScopeKind, source))
            {
                throw new InvalidOperationException(
                    $"Two permission sources claim scope kind '{source.ScopeKind}': "
                        + $"{byKind[source.ScopeKind].GetType().Name} and {source.GetType().Name}."
                );
            }
        }

        _sources = byKind.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    ) => Source(scope).HasPermissionAsync(scope, permission, cancellationToken);

    public Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    ) => Source(scope).IsInRoleAsync(scope, role, cancellationToken);

    // A missing source is a wiring mistake, not a denied user. Failing loudly beats
    // returning false, which would look exactly like "correctly configured, no access"
    // and hide the misconfiguration behind a plausible answer.
    private IPermissionSource Source(PermissionScope scope) =>
        _sources.TryGetValue(scope.Kind, out var source)
            ? source
            : throw new InvalidOperationException(
                $"No permission source is registered for scope kind '{scope.Kind}'. "
                    + $"Registered kinds: {(_sources.Count is 0 ? "(none)" : string.Join(", ", _sources.Keys))}."
            );
}
