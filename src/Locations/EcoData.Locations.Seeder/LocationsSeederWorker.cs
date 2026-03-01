using System.Text.Json;
using EcoData.Locations.Database;
using EcoData.Locations.Database.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace EcoData.Locations.Seeder;

public sealed class LocationsSeederWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<LocationsSeederWorker> logger
) : BackgroundService
{
    private const string PuertoRicoStateFips = "72";
    private const string PuertoRicoStateCode = "PR";
    private const string PuertoRicoStateName = "Puerto Rico";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            await MigrateAndSeedAsync(scope.ServiceProvider, stoppingToken);

            logger.LogInformation("Locations database migrations and seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the Locations database.");
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task MigrateAndSeedAsync(
        IServiceProvider services,
        CancellationToken stoppingToken)
    {
        var context = services.GetRequiredService<LocationsDbContext>();

        logger.LogInformation("Applying Locations database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Locations database migrations applied.");

        await SeedPuertoRicoAsync(context, stoppingToken);
    }

    private async Task SeedPuertoRicoAsync(
        LocationsDbContext context,
        CancellationToken stoppingToken)
    {
        var existingState = await context.States
            .FirstOrDefaultAsync(s => s.Code == PuertoRicoStateCode, stoppingToken);

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
            Name = PuertoRicoStateName,
            Code = PuertoRicoStateCode,
            FipsCode = PuertoRicoStateFips,
            Boundary = null,
            CreatedAt = now
        };

        context.States.Add(state);
        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Created state: {StateName}", state.Name);

        using var doc = JsonDocument.Parse(geoJsonContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("features", out var features))
        {
            var municipalities = new List<Municipality>();
            var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var properties))
                    continue;

                if (!feature.TryGetProperty("geometry", out var geometryElement))
                    continue;

                var stateFips = properties.GetProperty("STATE").GetString();
                if (stateFips != PuertoRicoStateFips)
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
                    CreatedAt = now
                };

                municipalities.Add(municipality);
            }

            context.Municipalities.AddRange(municipalities);
            await context.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Seeded {Count} municipalities for Puerto Rico", municipalities.Count);
        }
    }
}
