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
}
