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

public sealed class VirginIslandsBackfillWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<VirginIslandsBackfillWorker> logger
) : BackgroundService
{
    private const string StateFips = "78";
    private const string StateCode = "VI";
    private const string StateName = "U.S. Virgin Islands";

    // Stamped onto every occurrence this job creates; the per-species skip check
    // reads it to tell these GBIF point records apart from the Puerto Rico data.
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

        var geoJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "usvi-islands.geojson");
        if (!File.Exists(geoJsonPath))
        {
            throw new FileNotFoundException("usvi-islands.geojson not found.", geoJsonPath);
        }

        var geoJsonContent = await File.ReadAllTextAsync(geoJsonPath, stoppingToken);
        var geoJsonReader = new GeoJsonReader();
        var now = DateTimeOffset.UtcNow;

        var existingState = await context.States.FirstOrDefaultAsync(
            s => s.Code == StateCode,
            stoppingToken
        );

        // Already seeded: refresh geometry in place rather than skipping. The first release
        // used Census's generalized web-display boundaries, which dropped every offshore cay
        // and traced a coarser coastline than Puerto Rico's; re-running now corrects that.
        if (existingState is not null)
        {
            await RefreshIslandBoundariesAsync(context, geoJsonContent, geoJsonReader, stoppingToken);
            return;
        }

        logger.LogInformation("Seeding U.S. Virgin Islands geography...");

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

    private async Task RefreshIslandBoundariesAsync(
        LocationsDbContext context,
        string geoJsonContent,
        GeoJsonReader geoJsonReader,
        CancellationToken stoppingToken
    )
    {
        // The context is registered NoTracking, so updates need tracking turned back on.
        var islands = await context
            .Municipalities.AsTracking()
            .Where(m => m.GeoJsonId.StartsWith(StateFips))
            .ToDictionaryAsync(m => m.GeoJsonId, stoppingToken);

        using var doc = JsonDocument.Parse(geoJsonContent);
        if (!doc.RootElement.TryGetProperty("features", out var features))
        {
            throw new InvalidOperationException("usvi-islands.geojson has no features array.");
        }

        var refreshed = 0;

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var properties))
                continue;

            if (!feature.TryGetProperty("geometry", out var geometryElement))
                continue;

            var geoJsonId = $"{properties.GetProperty("STATE").GetString()}"
                + $"{properties.GetProperty("COUNTY").GetString()}";

            if (!islands.TryGetValue(geoJsonId, out var island))
                continue;

            var boundary = geoJsonReader.Read<Geometry>(geometryElement.GetRawText());
            boundary.SRID = 4326;

            if (island.Boundary is not null && island.Boundary.EqualsTopologically(boundary))
                continue;

            var centroid = boundary.Centroid;
            island.Boundary = boundary;
            island.CentroidLatitude = (decimal)centroid.Y;
            island.CentroidLongitude = (decimal)centroid.X;
            refreshed++;
        }

        if (refreshed == 0)
        {
            logger.LogInformation("U.S. Virgin Islands geography already current. Skipping...");
            return;
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("Refreshed boundaries for {Count} U.S. Virgin Islands.", refreshed);
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

        var islandIds = islands.Values.ToHashSet();

        var seededSpecies = 0;
        var seededLocations = 0;
        var seededLinks = 0;
        var resyncedSpecies = 0;
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

            var existing = await context
                .SpeciesLocations.AsTracking()
                .Where(l =>
                    l.SpeciesId == species.Id
                    && l.Description != null
                    && l.Description.StartsWith(DescriptionPrefix)
                )
                .ToListAsync(stoppingToken);

            var expected = dto
                .Locations.Select(l => $"{DescriptionPrefix}{l.GbifId}")
                .ToHashSet(StringComparer.Ordinal);

            if (existing.Count > 0 && existing.Select(l => l.Description!).ToHashSet(StringComparer.Ordinal).SetEquals(expected))
                continue;

            if (existing.Count > 0)
            {
                // The shipped data changed — the first release assigned occurrences against
                // generalized boundaries that reached far offshore, so points kilometres out
                // to sea were accepted. Replace the set rather than accumulating both.
                context.SpeciesLocations.RemoveRange(existing);

                // Island links are rebuilt below from the new points; drop the old ones so a
                // species that lost every point on an island stops claiming it.
                var staleLinks = await context
                    .MunicipalitySpecies.AsTracking()
                    .Where(ms => ms.SpeciesId == species.Id && islandIds.Contains(ms.MunicipalityId))
                    .ToListAsync(stoppingToken);
                context.MunicipalitySpecies.RemoveRange(staleLinks);
                resyncedSpecies++;
            }

            var existingLinks = await context
                .MunicipalitySpecies.Where(ms =>
                    ms.SpeciesId == species.Id && !islandIds.Contains(ms.MunicipalityId)
                )
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
            logger.LogInformation("U.S. Virgin Islands occurrences already current. Skipping...");
            return;
        }

        logger.LogInformation(
            "Seeded {Locations} occurrences across {Species} species and {Links} island links "
                + "({Resynced} species replaced because the shipped data changed).",
            seededLocations,
            seededSpecies,
            seededLinks,
            resyncedSpecies
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
