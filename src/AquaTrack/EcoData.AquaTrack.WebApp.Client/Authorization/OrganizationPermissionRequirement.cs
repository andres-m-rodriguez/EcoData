using Microsoft.AspNetCore.Authorization;

namespace EcoData.AquaTrack.WebApp.Client.Authorization;

public sealed class OrganizationPermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
