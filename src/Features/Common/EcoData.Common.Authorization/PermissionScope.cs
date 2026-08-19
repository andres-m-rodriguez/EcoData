namespace EcoData.Common.Authorization;

/// <summary>
/// What a permission or role is being asked about. Scopes differ in <em>kind</em>, not just
/// in value: an organization is identified by a guid, while a global question has nothing
/// to identify at all.
/// </summary>
/// <remarks>
/// Build one through the factory members rather than the constructor — the kind strings are
/// matched against <c>IPermissionSource.ScopeKind</c>, so a typo would route to no source.
/// <see cref="Custom"/> exists for kinds this library does not define.
/// </remarks>
public readonly record struct PermissionScope(string Kind, string? Id)
{
    public const string GlobalKind = "global";
    public const string OrganizationKind = "organization";

    /// <summary>Not scoped to anything — "may this user do X anywhere".</summary>
    public static PermissionScope Global => new(GlobalKind, null);

    /// <summary>Within one organization.</summary>
    public static PermissionScope Organization(Guid organizationId) =>
        new(OrganizationKind, organizationId.ToString());

    /// <summary>
    /// A kind this library does not define. The host must register a source for it, or
    /// checks against it throw.
    /// </summary>
    public static PermissionScope Custom(string kind, string? id) => new(kind, id);
}
