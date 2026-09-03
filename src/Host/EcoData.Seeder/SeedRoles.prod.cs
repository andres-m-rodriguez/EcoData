using EcoData.Organization.Contracts;
using EcoData.Organization.Database;
using EcoData.Organization.Database.Models;
using Microsoft.EntityFrameworkCore;
using OrgPermissions = EcoData.Organization.Contracts.Permissions;
using SensorPermissions = EcoData.Sensors.Contracts.Permissions;
using WildlifePermissions = EcoData.Wildlife.Contracts.Permissions;

namespace EcoData.Seeder;

// Every organization gets the default roles with their starting keys; Inter Metro
// also gets FaunaFinder's. Grants are only ever added: a key an admin granted since
// stays, a key an admin removed comes back on the next run.
internal sealed partial class SeedRoles(OrganizationDbContext context, ILogger<SeedRoles> logger)
{
    private static readonly (string Role, string[] Permissions)[] DefaultRoles =
    [
        (
            DefaultOrganizationRoles.Owner,
            [
                OrgPermissions.Organization.Update,
                OrgPermissions.Organization.Delete,
                OrgPermissions.Organization.ManageMembers,
                SensorPermissions.Sensor.Read,
                SensorPermissions.Sensor.Create,
                SensorPermissions.Sensor.Update,
                SensorPermissions.Sensor.Delete,
                WildlifePermissions.Species.Read,
                WildlifePermissions.Occurrence.Submit,
                WildlifePermissions.Occurrence.Verify,
            ]
        ),
        (
            DefaultOrganizationRoles.Admin,
            [
                OrgPermissions.Organization.Update,
                OrgPermissions.Organization.ManageMembers,
                SensorPermissions.Sensor.Read,
                SensorPermissions.Sensor.Create,
                SensorPermissions.Sensor.Update,
                SensorPermissions.Sensor.Delete,
                WildlifePermissions.Species.Read,
                WildlifePermissions.Occurrence.Submit,
                WildlifePermissions.Occurrence.Verify,
            ]
        ),
        (
            DefaultOrganizationRoles.Contributor,
            [
                SensorPermissions.Sensor.Read,
                SensorPermissions.Sensor.Create,
                SensorPermissions.Sensor.Update,
                WildlifePermissions.Species.Read,
                WildlifePermissions.Occurrence.Submit,
            ]
        ),
        (
            DefaultOrganizationRoles.Viewer,
            [SensorPermissions.Sensor.Read, WildlifePermissions.Species.Read]
        ),
    ];

    // Names mirror FaunaFinderRoles in FaunaFinder.Server, which the seeder can't reference.
    private static readonly (string Role, string[] Permissions)[] FaunaFinderRoles =
    [
        ("Student", [WildlifePermissions.Species.Read, WildlifePermissions.Occurrence.Submit]),
        (
            "FaunaAdministrator",
            [OrgPermissions.Organization.ManageMembers, WildlifePermissions.Occurrence.Verify]
        ),
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        var organizations = await context
            .Organizations.Select(o => new { o.Id, o.Slug })
            .ToListAsync(ct);

        foreach (var organization in organizations)
        {
            await EnsureRolesAsync(organization.Id, organization.Slug, DefaultRoles, ct);

            if (organization.Slug == SeedOrganizations.InterMetroSlug)
                await EnsureRolesAsync(organization.Id, organization.Slug, FaunaFinderRoles, ct);
        }
    }

    private async Task EnsureRolesAsync(
        Guid organizationId,
        string slug,
        (string Role, string[] Permissions)[] roles,
        CancellationToken ct
    )
    {
        var existing = await context
            .OrganizationRoles.Where(r => r.OrganizationId == organizationId)
            .ToDictionaryAsync(r => r.Name, r => r.Id, ct);
        var now = DateTimeOffset.UtcNow;

        foreach (var (roleName, permissions) in roles)
        {
            if (!existing.TryGetValue(roleName, out var roleId))
            {
                roleId = Guid.CreateVersion7();
                context.OrganizationRoles.Add(
                    new OrganizationRole
                    {
                        Id = roleId,
                        OrganizationId = organizationId,
                        Name = roleName,
                        CreatedAt = now,
                    }
                );
                existing[roleName] = roleId;
                logger.LogInformation("Added {Role} role to organization '{Slug}'", roleName, slug);
            }

            var granted = await context
                .OrganizationRolePermissions.Where(p => p.RoleId == roleId)
                .Select(p => p.Permission)
                .ToHashSetAsync(ct);

            foreach (var permission in permissions.Where(p => !granted.Contains(p)))
            {
                context.OrganizationRolePermissions.Add(
                    new OrganizationRolePermission { RoleId = roleId, Permission = permission }
                );
                logger.LogInformation(
                    "Granted {Permission} to {Role} in organization '{Slug}'",
                    permission,
                    roleName,
                    slug
                );
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
