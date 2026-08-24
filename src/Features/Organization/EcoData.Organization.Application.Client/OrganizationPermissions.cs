using EcoData.Common.Authorization;
using EcoData.Organization.Contracts;

namespace EcoData.Organization.Application.Client;

public static class OrganizationPermissions
{
    public static readonly OrgPermission UpdateOrganization = new(Permissions.Organization.Update);

    public static readonly OrgPermission DeleteOrganization = new(Permissions.Organization.Delete);

    public static readonly OrgPermission ManageMembers = new(Permissions.Organization.ManageMembers);
}
