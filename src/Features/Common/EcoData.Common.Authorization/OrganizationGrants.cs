namespace EcoData.Common.Authorization;

// The snapshot shape for UI that renders many checks synchronously: every grant the
// caller holds in one organization, fetched once. HasAsync stays the shape for a
// single decision; a snapshot shapes UI only and is never the enforcement answer.
public sealed record OrganizationGrants(IReadOnlySet<string> Permissions, bool IsGlobalAdmin)
{
    public static readonly OrganizationGrants None = new(new HashSet<string>(), IsGlobalAdmin: false);
}
