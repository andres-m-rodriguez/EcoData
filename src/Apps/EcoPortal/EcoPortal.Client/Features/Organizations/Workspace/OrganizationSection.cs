using EcoData.Ui.Workspace;
using MudBlazor;
using OrganizationPermissions = EcoData.Organization.Contracts.Permissions;
using SensorPermissions = EcoData.Sensors.Contracts.Permissions;

namespace EcoPortal.Client.Features.Organizations.Workspace;

public enum OrganizationSection
{
    Overview,
    Sensors,
    Team,
    Settings,
}

public static class OrganizationSections
{
    public static string Path(Guid organizationId, OrganizationSection section) => section switch
    {
        OrganizationSection.Overview => $"/organizations/{organizationId}",
        OrganizationSection.Sensors => $"/organizations/{organizationId}/sensors",
        OrganizationSection.Team => $"/organizations/{organizationId}/team",
        OrganizationSection.Settings => $"/organizations/{organizationId}/settings",
        _ => $"/organizations/{organizationId}",
    };

    public static string Key(OrganizationSection section) => section.ToString().ToLowerInvariant();

    // The {Section} route value (current and legacy spellings) → its section.
    public static OrganizationSection FromSegment(string? segment) => segment?.ToLowerInvariant() switch
    {
        "sensors" => OrganizationSection.Sensors,
        "team" or "members" or "requests" or "blocked-users" or "roles" => OrganizationSection.Team,
        "settings" or "edit" => OrganizationSection.Settings,
        _ => OrganizationSection.Overview,
    };

    public static IReadOnlyList<UiRailLink> Links(OrganizationContext context, OrganizationCounts? counts)
    {
        var id = context.Organization.Id;
        var links = new List<UiRailLink>
        {
            new(Key(OrganizationSection.Overview), "Overview", Path(id, OrganizationSection.Overview), Icons.Material.Outlined.Dashboard),
        };

        if (context.Can(SensorPermissions.Sensor.Read))
            links.Add(new(Key(OrganizationSection.Sensors), "Sensors", Path(id, OrganizationSection.Sensors), Icons.Material.Outlined.Sensors, Count: counts?.Sensors?.ToString()));

        if (context.Can(OrganizationPermissions.Organization.ManageMembers))
            links.Add(new(Key(OrganizationSection.Team), "Team", Path(id, OrganizationSection.Team), Icons.Material.Outlined.Group, Count: counts?.Members?.ToString(), Badge: counts?.PendingRequests ?? 0));

        if (context.Can(OrganizationPermissions.Organization.Update))
            links.Add(new(Key(OrganizationSection.Settings), "Settings", Path(id, OrganizationSection.Settings), Icons.Material.Outlined.Settings));

        return links;
    }
}
