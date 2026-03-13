using Microsoft.AspNetCore.Authorization;

namespace EcoPortal.Client.Authorization;

public sealed class OrganizationPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
