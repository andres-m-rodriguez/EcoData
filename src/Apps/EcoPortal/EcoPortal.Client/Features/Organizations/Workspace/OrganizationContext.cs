using EcoData.Organization.Contracts.Dtos;

namespace EcoPortal.Client.Features.Organizations.Workspace;

public sealed record OrganizationContext(
    OrganizationDtoForDetail Organization,
    IReadOnlySet<string> Permissions,
    bool IsGlobalAdmin,
    string? RoleName
)
{
    public bool IsMember => RoleName is not null || IsGlobalAdmin;

    public bool Can(string permission) => IsGlobalAdmin || Permissions.Contains(permission);
}

public sealed record OrganizationCounts(int? Sensors, int? Members, int? PendingRequests);
