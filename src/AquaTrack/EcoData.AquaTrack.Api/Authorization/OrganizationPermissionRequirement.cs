using Microsoft.AspNetCore.Authorization;

namespace EcoData.AquaTrack.Api.Authorization;

public sealed class OrganizationPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
