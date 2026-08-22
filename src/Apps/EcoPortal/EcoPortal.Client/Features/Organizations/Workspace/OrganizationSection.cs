using EcoData.Ui.Workspace;
using MudBlazor;
using OrganizationPermissions = EcoData.Organization.Contracts.Permissions;
using SensorPermissions = EcoData.Sensors.Contracts.Permissions;

namespace EcoPortal.Client.Features.Organizations.Workspace;

public enum OrganizationSection
{
    Overview,
    Sensors,
    Map,
    Team,
    Settings,
}

// The five places inside an organization, in rail order. A section that needs
// a permission the caller lacks is left out of the nav rather than shown and
// refused; the page behind it still carries its own [OrganizationPermission].
public static class OrganizationSections
{
    public static string Path(Guid organizationId, OrganizationSection section) => section switch
    {
        OrganizationSection.Overview => $"/organizations/{organizationId}",
        OrganizationSection.Sensors => $"/organizations/{organizationId}/sensors",
        OrganizationSection.Map => $"/organizations/{organizationId}/map",
        OrganizationSection.Team => $"/organizations/{organizationId}/team",
        OrganizationSection.Settings => $"/organizations/{organizationId}/settings",
        _ => $"/organizations/{organizationId}",
    };

    public static string Key(OrganizationSection section) => section.ToString().ToLowerInvariant();

    public static IReadOnlyList<UiRailLink> Links(OrganizationContext context, OrganizationCounts? counts)
    {
        var id = context.Organization.Id;
        var links = new List<UiRailLink>
        {
            new(Key(OrganizationSection.Overview), "Overview", Path(id, OrganizationSection.Overview), Icons.Material.Outlined.Dashboard),
        };

        if (context.Can(SensorPermissions.Sensor.Read))
        {
            links.Add(new(Key(OrganizationSection.Sensors), "Sensors", Path(id, OrganizationSection.Sensors), Icons.Material.Outlined.Sensors, Count: counts?.Sensors?.ToString()));
            links.Add(new(Key(OrganizationSection.Map), "Map", Path(id, OrganizationSection.Map), Icons.Material.Outlined.Map));
        }

        if (context.Can(OrganizationPermissions.Organization.ManageMembers))
        {
            links.Add(new(Key(OrganizationSection.Team), "Team", Path(id, OrganizationSection.Team), Icons.Material.Outlined.Group, Count: counts?.Members?.ToString(), Badge: counts?.PendingRequests ?? 0));
        }

        if (context.Can(OrganizationPermissions.Organization.Update))
        {
            links.Add(new(Key(OrganizationSection.Settings), "Settings", Path(id, OrganizationSection.Settings), Icons.Material.Outlined.Settings));
        }

        return links;
    }
}
