using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Database;
using EcoData.Organization.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.IntegrationTests;

/// <summary>
/// Retrieves test data seeded by the Seeder project.
/// The Seeder seeds "Test Org" when SEED_TEST_DATA=true (set via AppHost in Testing environment).
/// </summary>
public sealed class TestSeeder(IServiceProvider services)
{
    private const string TestOrgName = "Test Org";

    public async Task<(
        OrganizationsTestStore Organizations,
        LocationsTestStore Locations
    )> SeedAsync(CancellationToken ct = default)
    {
        var organizations = await GetOrganizationAsync(ct);
        var locations = await GetLocationsAsync(ct);

        return (organizations, locations);
    }

    private async Task<OrganizationsTestStore> GetOrganizationAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();

        var org = await context.Organizations.FirstOrDefaultAsync(
            o => o.Name == TestOrgName,
            ct
        ) ?? throw new InvalidOperationException(
            $"Test organization '{TestOrgName}' not found. Ensure Seeder ran with SEED_TEST_DATA=true.");

        return new OrganizationsTestStore(org.Id, TestOrgName);
    }

    private async Task<LocationsTestStore> GetLocationsAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LocationsDbContext>();

        var municipality = await context.Municipalities.FirstAsync(ct);

        return new LocationsTestStore(
            municipality.Id,
            municipality.CentroidLatitude,
            municipality.CentroidLongitude
        );
    }
}
