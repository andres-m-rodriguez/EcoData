using EcoData.IntegrationTests.Stores;
using EcoData.Locations.Database;
using EcoData.Organization.Database;
using EcoData.Organization.DataAccess.Slugs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.IntegrationTests;

public sealed class TestSeeder(IServiceProvider services)
{
    private const string TestOrgName = "Test Org";

    public async Task<(
        OrganizationsTestStore Organizations,
        LocationsTestStore Locations
    )> SeedAsync(CancellationToken ct = default)
    {
        var organizations = await SeedOrganizationAsync(ct);
        var locations = await SeedLocationsAsync(ct);

        return (organizations, locations);
    }

    private async Task<OrganizationsTestStore> SeedOrganizationAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();

        var existingOrg = await context.Organizations.FirstOrDefaultAsync(
            o => o.Name == TestOrgName,
            ct
        );

        if (existingOrg is not null)
            return new OrganizationsTestStore(existingOrg.Id, TestOrgName);

        var org = new EcoData.Organization.Database.Models.Organization
        {
            Id = Guid.CreateVersion7(),
            Name = TestOrgName,
            Slug = SlugGenerator.FromName(TestOrgName),
            Tagline = null,
            ProfilePictureUrl = null,
            CardPictureUrl = null,
            AboutUs = null,
            WebsiteUrl = null,
            Location = null,
            FoundedYear = null,
            LegalStatus = null,
            TaxId = null,
            PrimaryColor = null,
            AccentColor = null,
            ContactEmail = null,
            Type = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        context.Organizations.Add(org);
        await context.SaveChangesAsync(ct);

        return new OrganizationsTestStore(org.Id, TestOrgName);
    }

    private async Task<LocationsTestStore> SeedLocationsAsync(CancellationToken ct)
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
