using Microsoft.AspNetCore.Authorization;

namespace EcoData.Organization.Api.Authorization;

public sealed class OrganizationPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
