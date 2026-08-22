using EcoData.Organization.Contracts.Dtos;

namespace EcoPortal.Client.Features.Organizations.Workspace;

// Everything the workspace knows about one organization for the signed-in
// user, loaded once per organization and cascaded to the section pages.
public sealed record OrganizationContext(
    OrganizationDtoForDetail Organization,
    IReadOnlySet<string> Permissions,
    bool IsGlobalAdmin,
    string? RoleName
)
{
    // A member holds a role; a GlobalAdmin is treated as one for the workspace.
    public bool IsMember => RoleName is not null || IsGlobalAdmin;

    public bool Can(string permission) => IsGlobalAdmin || Permissions.Contains(permission);
}

public sealed record OrganizationCounts(int? Sensors, int? Members, int? PendingRequests);
