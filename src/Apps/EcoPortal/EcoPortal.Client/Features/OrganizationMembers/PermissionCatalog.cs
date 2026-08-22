using OrganizationPermissions = EcoData.Organization.Contracts.Permissions;
using SensorPermissions = EcoData.Sensors.Contracts.Permissions;
using WildlifePermissions = EcoData.Wildlife.Contracts.Permissions;

namespace EcoPortal.Client.Features.OrganizationMembers;

public sealed record PermissionOption(string Key, string Label, string Description);

public sealed record PermissionGroup(string Title, IReadOnlyList<PermissionOption> Options);

// The app is the one place that sees every module, so it is where the keys each module
// declares are gathered into something a person can pick from.
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionGroup> Groups =
    [
        new(
            "Organization",
            [
                new(
                    OrganizationPermissions.Organization.Update,
                    "Edit organization",
                    "Change the profile, branding and contact details."
                ),
                new(
                    OrganizationPermissions.Organization.ManageMembers,
                    "Manage members and roles",
                    "Review access requests, change member roles, edit roles."
                ),
                new(
                    OrganizationPermissions.Organization.Delete,
                    "Delete organization",
                    "Remove the organization and everything it owns."
                ),
            ]
        ),
        new(
            "Sensors",
            [
                new(SensorPermissions.Sensor.Read, "View sensors", "See sensors and their readings."),
                new(SensorPermissions.Sensor.Create, "Register sensors", "Add new sensors."),
                new(
                    SensorPermissions.Sensor.Update,
                    "Configure sensors",
                    "Edit sensor details and health settings."
                ),
                new(SensorPermissions.Sensor.Delete, "Delete sensors", "Remove sensors."),
            ]
        ),
        new(
            "Wildlife",
            [
                new(
                    WildlifePermissions.Species.Read,
                    "View species",
                    "Browse the species catalogue."
                ),
                new(
                    WildlifePermissions.Occurrence.Submit,
                    "Submit sightings",
                    "Record species occurrences."
                ),
                new(
                    WildlifePermissions.Occurrence.Verify,
                    "Verify sightings",
                    "Confirm or reject submitted occurrences."
                ),
            ]
        ),
    ];

    private static readonly Dictionary<string, PermissionOption> ByKey = Groups
        .SelectMany(g => g.Options)
        .ToDictionary(o => o.Key, StringComparer.Ordinal);

    // Keys the catalogue does not know (older data, another module) still show as themselves.
    public static string Label(string key) => ByKey.TryGetValue(key, out var option) ? option.Label : key;
}
