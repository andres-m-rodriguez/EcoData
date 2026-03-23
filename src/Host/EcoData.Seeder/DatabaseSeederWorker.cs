using System.Text.Json;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using EcoData.Locations.Database;
using EcoData.Locations.Database.Models;
using EcoData.Organization.Database;
using EcoData.Sensors.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace EcoData.Seeder;

public sealed class DatabaseSeederWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<DatabaseSeederWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider;

            await MigrateOrganizationAsync(services, stoppingToken);
            await MigrateSensorsAsync(services, stoppingToken);
            await MigrateIdentityAsync(services, stoppingToken);
            await MigrateLocationsAsync(services, stoppingToken);

            await SeedAdminUserAsync(services, stoppingToken);
            await SeedLocationsAsync(services, stoppingToken);
            await SeedOrganizationRolesAsync(services, stoppingToken);

            if (IsTestEnvironment())
            {
                logger.LogInformation("Test environment detected. Seeding test data...");
                await SeedTestOrganizationAsync(services, stoppingToken);
            }

            logger.LogInformation("All database migrations and seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the databases.");
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task MigrateOrganizationAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<OrganizationDbContext>();
        logger.LogInformation("Applying Organization database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Organization database migrations applied.");
    }

    private async Task MigrateSensorsAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<SensorsDbContext>();
        logger.LogInformation("Applying Sensors database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Sensors database migrations applied.");
    }

    private async Task MigrateIdentityAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<IdentityDbContext>();
        logger.LogInformation("Applying Identity database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Identity database migrations applied.");
    }

    private async Task MigrateLocationsAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<LocationsDbContext>();
        logger.LogInformation("Applying Locations database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Locations database migrations applied.");
    }

    private async Task SeedAdminUserAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<IdentityDbContext>();

        const string adminEmail = "admin@gmail.com";
        var existingAdmin = await context.Users.FirstOrDefaultAsync(
            u => u.Email == adminEmail,
            stoppingToken
        );

        if (existingAdmin is not null)
        {
            logger.LogInformation("Admin user already exists. Skipping...");
            return;
        }

        logger.LogInformation("Creating admin user...");

        var now = DateTimeOffset.UtcNow;
        var adminUser = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = "Admin",
            GlobalRole = GlobalRole.GlobalAdmin,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = now,
        };

        var passwordHasher = new PasswordHasher<User>();
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");

        context.Users.Add(adminUser);
        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Admin user created: {Email}", adminEmail);
    }

    private async Task SeedLocationsAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<LocationsDbContext>();

        const string puertoRicoStateFips = "72";
        const string puertoRicoStateCode = "PR";
        const string puertoRicoStateName = "Puerto Rico";

        var existingState = await context.States.FirstOrDefaultAsync(
            s => s.Code == puertoRicoStateCode,
            stoppingToken
        );

        if (existingState is not null)
        {
            logger.LogInformation("Puerto Rico data already seeded. Skipping...");
            return;
        }

        logger.LogInformation("Seeding Puerto Rico data...");

        var geoJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "pr-municipios.geojson");
        if (!File.Exists(geoJsonPath))
        {
            logger.LogError("Puerto Rico GeoJSON file not found at {Path}", geoJsonPath);
            return;
        }

        var geoJsonContent = await File.ReadAllTextAsync(geoJsonPath, stoppingToken);
        var geoJsonReader = new GeoJsonReader();

        var now = DateTimeOffset.UtcNow;
        var stateId = Guid.CreateVersion7();

        var state = new State
        {
            Id = stateId,
            Name = puertoRicoStateName,
            Code = puertoRicoStateCode,
            FipsCode = puertoRicoStateFips,
            Boundary = null,
            CreatedAt = now,
        };

        context.States.Add(state);
        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Created state: {StateName}", state.Name);

        using var doc = JsonDocument.Parse(geoJsonContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("features", out var features))
        {
            var municipalities = new List<Municipality>();

            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var properties))
                    continue;

                if (!feature.TryGetProperty("geometry", out var geometryElement))
                    continue;

                var stateFips = properties.GetProperty("STATE").GetString();
                if (stateFips != puertoRicoStateFips)
                    continue;

                var countyFips = properties.GetProperty("COUNTY").GetString() ?? "";
                var name = properties.GetProperty("NAME").GetString() ?? "";
                var geoJsonId = $"{stateFips}{countyFips}";

                var geometryJson = geometryElement.GetRawText();
                Geometry? boundary = null;
                decimal centroidLat = 0;
                decimal centroidLon = 0;

                try
                {
                    boundary = geoJsonReader.Read<Geometry>(geometryJson);
                    if (boundary is not null)
                    {
                        boundary.SRID = 4326;
                        var centroid = boundary.Centroid;
                        centroidLat = (decimal)centroid.Y;
                        centroidLon = (decimal)centroid.X;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse geometry for municipality {Name}", name);
                }

                var municipality = new Municipality
                {
                    Id = Guid.CreateVersion7(),
                    StateId = stateId,
                    Name = name,
                    GeoJsonId = geoJsonId,
                    CountyFipsCode = countyFips,
                    Boundary = boundary,
                    CentroidLatitude = centroidLat,
                    CentroidLongitude = centroidLon,
                    CreatedAt = now,
                };

                municipalities.Add(municipality);
            }

            context.Municipalities.AddRange(municipalities);
            await context.SaveChangesAsync(stoppingToken);

            logger.LogInformation(
                "Seeded {Count} municipalities for Puerto Rico",
                municipalities.Count
            );
        }
    }

    private async Task SeedOrganizationRolesAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<OrganizationDbContext>();

        // Get all organizations that don't have a Contributor role
        var organizationsWithoutContributor = await context
            .Organizations.Where(o =>
                !context.OrganizationRoles.Any(r =>
                    r.OrganizationId == o.Id && r.Name == "Contributor"
                )
            )
            .Select(o => o.Id)
            .ToListAsync(stoppingToken);

        if (organizationsWithoutContributor.Count == 0)
        {
            logger.LogInformation("All organizations have Contributor role. Skipping...");
            return;
        }

        logger.LogInformation(
            "Adding Contributor role to {Count} organizations...",
            organizationsWithoutContributor.Count
        );

        var now = DateTimeOffset.UtcNow;
        foreach (var organizationId in organizationsWithoutContributor)
        {
            context.OrganizationRoles.Add(
                new Organization.Database.Models.OrganizationRole
                {
                    Id = Guid.CreateVersion7(),
                    OrganizationId = organizationId,
                    Name = "Contributor",
                    CreatedAt = now,
                }
            );
        }

        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation(
            "Added Contributor role to {Count} organizations",
            organizationsWithoutContributor.Count
        );
    }

    private static bool IsTestEnvironment() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing" ||
        Environment.GetEnvironmentVariable("SEED_TEST_DATA") == "true";

    private async Task SeedTestOrganizationAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<OrganizationDbContext>();

        const string testOrgName = "Test Org";
        var existingOrg = await context.Organizations.FirstOrDefaultAsync(
            o => o.Name == testOrgName,
            stoppingToken
        );

        if (existingOrg is not null)
        {
            logger.LogInformation("Test organization already exists. Skipping...");
            return;
        }

        logger.LogInformation("Creating test organization...");

        var now = DateTimeOffset.UtcNow;
        var org = new Organization.Database.Models.Organization
        {
            Id = Guid.CreateVersion7(),
            Name = testOrgName,
            ProfilePictureUrl = null,
            CardPictureUrl = null,
            AboutUs = "Test organization for integration tests",
            WebsiteUrl = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.Organizations.Add(org);

        // Add Contributor role for the test org
        context.OrganizationRoles.Add(
            new Organization.Database.Models.OrganizationRole
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = org.Id,
                Name = "Contributor",
                CreatedAt = now,
            }
        );

        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Test organization created: {Name}", testOrgName);
    }
}
