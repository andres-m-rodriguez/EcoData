using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Database;
using EcoData.Organization.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.IntegrationTests;

public static class TestSeeder
{
    private const string TestOrgName = "Test Org";

    public static async Task<(
        OrganizationsTestStore Organizations,
        LocationsTestStore Locations
    )> SeedAsync(DistributedApplication app, CancellationToken ct = default)
    {
        var organizationConnStr = await GetConnectionStringAsync(app, "organization", ct);
        var locationsConnStr = await GetConnectionStringAsync(app, "locations", ct);

        var organizations = await SeedOrganizationAsync(organizationConnStr!, ct);
        var locations = await SeedLocationsAsync(locationsConnStr!, ct);

        return (organizations, locations);
    }

    private static async Task<OrganizationsTestStore> SeedOrganizationAsync(
        string connectionString,
        CancellationToken ct
    )
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new OrganizationDbContext(options);

        var existingOrg = await context.Organizations.FirstOrDefaultAsync(
            o => o.Name == TestOrgName,
            ct
        );

        if (existingOrg is not null)
            return new OrganizationsTestStore(existingOrg.Id, TestOrgName);

        var org = new EcoData.Organization.Database.Models.Organization
        {
            Id = Guid.NewGuid(),
            Name = TestOrgName,
            ProfilePictureUrl = null,
            CardPictureUrl = null,
            AboutUs = null,
            WebsiteUrl = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        context.Organizations.Add(org);
        await context.SaveChangesAsync(ct);

        return new OrganizationsTestStore(org.Id, TestOrgName);
    }

    private static async Task<LocationsTestStore> SeedLocationsAsync(
        string connectionString,
        CancellationToken ct
    )
    {
        var options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new LocationsDbContext(options);

        var municipality = await context.Municipalities.FirstAsync(ct);

        return new LocationsTestStore(
            municipality.Id,
            municipality.CentroidLatitude,
            municipality.CentroidLongitude
        );
    }

    private static async Task<string?> GetConnectionStringAsync(
        DistributedApplication app,
        string resourceName,
        CancellationToken ct
    )
    {
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource =
            model.Resources.SingleOrDefault(r => r.Name == resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        if (resource is not IResourceWithConnectionString connStrResource)
            throw new InvalidOperationException(
                $"Resource '{resourceName}' does not expose a connection string."
            );

        return await connStrResource.GetConnectionStringAsync(ct);
    }
}
