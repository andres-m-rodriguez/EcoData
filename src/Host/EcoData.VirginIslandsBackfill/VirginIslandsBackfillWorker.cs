using System.Text.Json;
using System.Text.Json.Serialization;
using EcoData.Locations.Database;
using EcoData.Locations.Database.Models;
using EcoData.Wildlife.Database;
using EcoData.Wildlife.Database.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace EcoData.VirginIslandsBackfill;

/// <summary>
/// One-shot job that adds the U.S. Virgin Islands as a second jurisdiction and attaches the
/// USVI occurrences of species that already exist in the catalogue.
/// </summary>
/// <remarks>
/// The catalogue is the FWS Caribbean Ecological Services Field Office listed-species list,
/// whose jurisdiction is Puerto Rico <em>and</em> the U.S. Virgin Islands. Several records —
/// Ameiva polops, Agave eggersiana, Solanum conocarpum — are St. Croix or St. John taxa that
/// do not occur in Puerto Rico at all, so they had nowhere to be attributed and reported zero
/// municipios.
///
/// This is a separate job rather than a step in the seeder because the seeder's location
/// seeding returns early once Puerto Rico exists, so an existing database would never reach
/// it. It also runs no migrations — the seeder owns those.
///
/// Safe to run repeatedly: the geography step is a no-op once the VI state row exists, and
/// the species step skips any species that already carries a USVI-sourced location.
/// </remarks>
public sealed class VirginIslandsBackfillWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<VirginIslandsBackfillWorker> logger
) : BackgroundService
{
    private const string StateFips = "78";
    private const string StateCode = "VI";
    private const string StateName = "U.S. Virgin Islands";

    /// <summary>
    /// Stamped onto every occurrence this job creates. The USVI records are GBIF point
    /// records, whereas the Puerto Rico data is generalized occurrence polygons from the
    /// Natural Heritage element-occurrence layer. Keeping the provenance on the row is what
    /// makes the two separable, and is what the per-species skip check reads.
    /// </summary>
    private const string DescriptionPrefix = "GBIF occurrence ";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var services = scope.ServiceProvider;

            await SeedIslandsAsync(services, stoppingToken);
            await SeedOccurrencesAsync(services, stoppingToken);

            logger.LogInformation("U.S. Virgin Islands backfill completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the U.S. Virgin Islands backfill.");
            // StopApplication lets Run() return normally, so without this the container
            // exits 0 and the Container App Job reports Succeeded on a failed backfill.
            Environment.ExitCode = 1;
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task SeedIslandsAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<LocationsDbContext>();

        if (await context.States.AnyAsync(s => s.Code == StateCode, stoppingToken))
        {
            logger.LogInformation("U.S. Virgin Islands already seeded. Skipping...");
            return;
        }

        var geoJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "usvi-islands.geojson");
        if (!File.Exists(geoJsonPath))
        {
            throw new FileNotFoundException("usvi-islands.geojson not found.", geoJsonPath);
        }

        logger.LogInformation("Seeding U.S. Virgin Islands geography...");

        var geoJsonContent = await File.ReadAllTextAsync(geoJsonPath, stoppingToken);
        var geoJsonReader = new GeoJsonReader();
        var now = DateTimeOffset.UtcNow;
        var stateId = Guid.CreateVersion7();

        context.States.Add(
            new State
            {
                Id = stateId,
                Name = StateName,
                Code = StateCode,
                FipsCode = StateFips,
                Boundary = null,
                CreatedAt = now,
            }
        );
        await context.SaveChangesAsync(stoppingToken);

        using var doc = JsonDocument.Parse(geoJsonContent);
        if (!doc.RootElement.TryGetProperty("features", out var features))
        {
            throw new InvalidOperationException("usvi-islands.geojson has no features array.");
        }

        var islands = new List<Municipality>();

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var properties))
                continue;

            if (!feature.TryGetProperty("geometry", out var geometryElement))
                continue;

            if (properties.GetProperty("STATE").GetString() != StateFips)
                continue;

            var countyFips = properties.GetProperty("COUNTY").GetString() ?? "";
            var name = properties.GetProperty("NAME").GetString() ?? "";

            var boundary = geoJsonReader.Read<Geometry>(geometryElement.GetRawText());
            boundary.SRID = 4326;
            var centroid = boundary.Centroid;

            islands.Add(
                new Municipality
                {
                    Id = Guid.CreateVersion7(),
                    StateId = stateId,
                    Name = name,
                    GeoJsonId = $"{StateFips}{countyFips}",
                    CountyFipsCode = countyFips,
                    Boundary = boundary,
                    CentroidLatitude = (decimal)centroid.Y,
                    CentroidLongitude = (decimal)centroid.X,
                    CreatedAt = now,
                }
            );
        }

        context.Municipalities.AddRange(islands);
        await context.SaveChangesAsync(stoppingToken);

        logger.LogInformation("Seeded {Count} U.S. Virgin Islands islands.", islands.Count);
    }

    private async Task SeedOccurrencesAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<WildlifeDbContext>();
        var locationsContext = services.GetRequiredService<LocationsDbContext>();

        var jsonPath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "usvi_species_locations.json"
        );
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException("usvi_species_locations.json not found.", jsonPath);
        }

        var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
        var payload = JsonSerializer.Deserialize<UsviLocationsDto>(json, JsonOptions);
        if (payload?.Species is not { Count: > 0 })
        {
            logger.LogWarning("usvi_species_locations.json contained no species. Skipping.");
            return;
        }

        var islands = await locationsContext
            .Municipalities.Where(m => m.GeoJsonId.StartsWith(StateFips))
            .ToDictionaryAsync(m => m.GeoJsonId, m => m.Id, stoppingToken);

        if (islands.Count == 0)
        {
            throw new InvalidOperationException(
                "No U.S. Virgin Islands municipalities found; the geography step must run first."
            );
        }

        var seededSpecies = 0;
        var seededLocations = 0;
        var seededLinks = 0;
        var unknown = new List<string>();

        foreach (var dto in payload.Species)
        {
            if (dto.Locations is not { Count: > 0 })
                continue;

            var species = await context.Species.FirstOrDefaultAsync(
                s => s.ScientificName == dto.ScientificName,
                stoppingToken
            );

            if (species is null)
            {
                unknown.Add(dto.ScientificName);
                continue;
            }

            var alreadySeeded = await context.SpeciesLocations.AnyAsync(
                l =>
                    l.SpeciesId == species.Id
                    && l.Description != null
                    && l.Description.StartsWith(DescriptionPrefix),
                stoppingToken
            );

            if (alreadySeeded)
                continue;

            var existingLinks = await context
                .MunicipalitySpecies.Where(ms => ms.SpeciesId == species.Id)
                .Select(ms => ms.MunicipalityId)
                .ToHashSetAsync(stoppingToken);

            foreach (var location in dto.Locations)
            {
                context.SpeciesLocations.Add(
                    new SpeciesLocation
                    {
                        Id = Guid.CreateVersion7(),
                        SpeciesId = species.Id,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        RadiusMeters = location.RadiusMeters,
                        Description = $"{DescriptionPrefix}{location.GbifId}",
                    }
                );
                seededLocations++;

                if (
                    !islands.TryGetValue(location.IslandGeoJsonId, out var islandId)
                    || !existingLinks.Add(islandId)
                )
                {
                    continue;
                }

                context.MunicipalitySpecies.Add(
                    new MunicipalitySpecies
                    {
                        Id = Guid.CreateVersion7(),
                        MunicipalityId = islandId,
                        SpeciesId = species.Id,
                    }
                );
                seededLinks++;
            }

            seededSpecies++;
            await context.SaveChangesAsync(stoppingToken);
        }

        foreach (var name in unknown)
        {
            logger.LogWarning("USVI locations reference unknown species {Name}. Skipped.", name);
        }

        if (seededSpecies == 0)
        {
            logger.LogInformation("U.S. Virgin Islands occurrences already seeded. Skipping...");
            return;
        }

        logger.LogInformation(
            "Seeded {Locations} occurrences across {Species} species and {Links} island links.",
            seededLocations,
            seededSpecies,
            seededLinks
        );
    }

    private sealed class UsviLocationsDto
    {
        [JsonPropertyName("species")]
        public List<UsviSpeciesDto>? Species { get; init; }
    }

    private sealed class UsviSpeciesDto
    {
        [JsonPropertyName("scientificName")]
        public required string ScientificName { get; init; }

        [JsonPropertyName("locations")]
        public List<UsviLocationDto>? Locations { get; init; }
    }

    private sealed class UsviLocationDto
    {
        [JsonPropertyName("latitude")]
        public required double Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public required double Longitude { get; init; }

        [JsonPropertyName("radiusMeters")]
        public required double RadiusMeters { get; init; }

        [JsonPropertyName("islandGeoJsonId")]
        public required string IslandGeoJsonId { get; init; }

        [JsonPropertyName("gbifId")]
        public long? GbifId { get; init; }
    }
}
