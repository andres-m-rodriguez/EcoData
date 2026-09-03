using EcoData.Organization.Contracts;

namespace EcoData.Seeder;

internal sealed partial class SeedOrganizations
{
    // A second org so one account can belong to two, and a third with no seeded
    // members so the join-an-organization flow has somewhere to go.
    public const string CoastalLabSlug = "coastal-lab";
    public const string RioPiedrasWatchersSlug = "rio-piedras-watchers";

    public async Task SeedDevelopmentAsync(CancellationToken ct)
    {
        await EnsureAsync(CoastalLabSlug, "Coastal Sensor Lab", OrganizationType.ResearchInstitute, ct);
        await EnsureAsync(RioPiedrasWatchersSlug, "Rio Piedras Watchers", OrganizationType.CitizenScience, ct);
    }
}
