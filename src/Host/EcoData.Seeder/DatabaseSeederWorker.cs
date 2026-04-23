using System.Text.Json;
using System.Text.Json.Serialization;
using EcoData.Common.i18n;
using EcoData.Identity.Database;
using EcoData.Identity.Database.Models;
using EcoData.Locations.Database;
using EcoData.Locations.Database.Models;
using EcoData.Organization.Database;
using EcoData.Sensors.Database;
using EcoData.Wildlife.Database;
using EcoData.Wildlife.Database.Models;
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
            await MigrateWildlifeAsync(services, stoppingToken);

            await SeedAdminUserAsync(services, stoppingToken);
            await SeedLocationsAsync(services, stoppingToken);
            await SeedOrganizationRolesAsync(services, stoppingToken);
            await SeedWildlifeAsync(services, stoppingToken);

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

    private async Task MigrateWildlifeAsync(
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var context = services.GetRequiredService<WildlifeDbContext>();
        logger.LogInformation("Applying Wildlife database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Wildlife database migrations applied.");
    }

    private async Task SeedWildlifeAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        var context = services.GetRequiredService<WildlifeDbContext>();
        var locationsContext = services.GetRequiredService<LocationsDbContext>();

        await SeedSpeciesCategoriesAsync(context, stoppingToken);
        await SeedNrcsPracticesAsync(context, stoppingToken);
        await SeedFwsActionsAsync(context, stoppingToken);
        await SeedSpeciesAsync(context, locationsContext, stoppingToken);
        await SeedFwsLinksAsync(context, stoppingToken);
    }

    private async Task SeedSpeciesCategoriesAsync(
        WildlifeDbContext context,
        CancellationToken stoppingToken
    )
    {
        var defaultCategories = new[]
        {
            ("bird", "Bird", "Ave"),
            ("mammal", "Mammal", "Mamífero"),
            ("reptile", "Reptile", "Reptil"),
            ("amphibian", "Amphibian", "Anfibio"),
            ("fish", "Fish", "Pez"),
            ("invertebrate", "Invertebrate", "Invertebrado"),
            ("plant", "Plant", "Planta"),
            ("fern", "Fern", "Helecho"),
        };

        var existing = await context.SpeciesCategories.ToDictionaryAsync(
            c => c.Code,
            c => c,
            stoppingToken
        );

        foreach (var (code, nameEn, nameEs) in defaultCategories)
        {
            if (!existing.ContainsKey(code))
            {
                context.SpeciesCategories.Add(
                    new SpeciesCategory
                    {
                        Id = Guid.CreateVersion7(),
                        Code = code,
                        Name = [new LocaleValue("en", nameEn), new LocaleValue("es", nameEs)],
                    }
                );
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("Species categories seeded.");
    }

    private async Task SeedNrcsPracticesAsync(
        WildlifeDbContext context,
        CancellationToken stoppingToken
    )
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "nrcs_practices.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("nrcs_practices.json not found. Skipping NRCS practices seeding.");
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
        var practices = JsonSerializer.Deserialize<List<NrcsPracticeDto>>(json, JsonOptions);
        if (practices is null)
            return;

        var existing = await context.NrcsPractices.ToDictionaryAsync(
            p => p.Code,
            p => p,
            stoppingToken
        );

        foreach (var dto in practices)
        {
            if (!existing.ContainsKey(dto.Code))
            {
                context.NrcsPractices.Add(
                    new NrcsPractice
                    {
                        Id = Guid.CreateVersion7(),
                        Code = dto.Code,
                        Name = dto.Name,
                    }
                );
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("NRCS practices seeded: {Count}", practices.Count);
    }

    private async Task SeedFwsActionsAsync(
        WildlifeDbContext context,
        CancellationToken stoppingToken
    )
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "fws_actions.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("fws_actions.json not found. Skipping FWS actions seeding.");
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
        var actions = JsonSerializer.Deserialize<List<FwsActionDto>>(json, JsonOptions);
        if (actions is null)
            return;

        var existing = await context.FwsActions.ToDictionaryAsync(
            a => a.Code,
            a => a,
            stoppingToken
        );

        foreach (var dto in actions)
        {
            if (!existing.ContainsKey(dto.Code))
            {
                context.FwsActions.Add(
                    new FwsAction
                    {
                        Id = Guid.CreateVersion7(),
                        Code = dto.Code,
                        Name = dto.Name,
                    }
                );
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("FWS actions seeded: {Count}", actions.Count);
    }

    private async Task SeedSpeciesAsync(
        WildlifeDbContext context,
        LocationsDbContext locationsContext,
        CancellationToken stoppingToken
    )
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "species.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("species.json not found. Skipping species seeding.");
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
        var speciesList = JsonSerializer.Deserialize<List<SpeciesDto>>(json, JsonOptions);
        if (speciesList is null)
            return;

        var existingSpecies = await context.Species.ToDictionaryAsync(
            s => s.ScientificName,
            s => s,
            stoppingToken
        );

        var categories = await context.SpeciesCategories.ToDictionaryAsync(
            c => c.Code,
            c => c,
            stoppingToken
        );

        // Map GeoJsonId to Municipality Guid from Locations database
        var municipalities = await locationsContext.Municipalities.ToDictionaryAsync(
            m => m.GeoJsonId,
            m => m.Id,
            stoppingToken
        );

        var seededCount = 0;
        foreach (var dto in speciesList)
        {
            Species species;

            if (existingSpecies.TryGetValue(dto.ScientificName, out var existing))
            {
                species = existing;

                // Update image if we have one and the existing doesn't
                if (species.ProfileImageData is null && !string.IsNullOrEmpty(dto.ImageBase64))
                {
                    species.ProfileImageData = Convert.FromBase64String(dto.ImageBase64);
                    species.ProfileImageContentType = dto.ImageContentType;
                }

                if (species.ImageSourceUrl is null && !string.IsNullOrEmpty(dto.ImageSourceUrl))
                {
                    species.ImageSourceUrl = dto.ImageSourceUrl;
                }
            }
            else
            {
                species = new Species
                {
                    Id = Guid.CreateVersion7(),
                    CommonName =
                    [
                        new LocaleValue("en", dto.CommonNameEn),
                        new LocaleValue("es", dto.CommonNameEs),
                    ],
                    ScientificName = dto.ScientificName,
                    ProfileImageData = string.IsNullOrEmpty(dto.ImageBase64)
                        ? null
                        : Convert.FromBase64String(dto.ImageBase64),
                    ProfileImageContentType = dto.ImageContentType,
                    ImageSourceUrl = dto.ImageSourceUrl,
                    IsFauna = dto.IsFauna,
                    ElCode = dto.ElCode ?? "",
                    GRank = dto.GRank ?? "",
                    SRank = dto.SRank ?? "",
                };
                context.Species.Add(species);
                await context.SaveChangesAsync(stoppingToken);
                existingSpecies[dto.ScientificName] = species;
                seededCount++;
            }

            // Link to municipalities
            if (dto.MunicipalityGeoJsonIds is { Count: > 0 })
            {
                var existingLinks = await context
                    .MunicipalitySpecies.Where(ms => ms.SpeciesId == species.Id)
                    .Select(ms => ms.MunicipalityId)
                    .ToHashSetAsync(stoppingToken);

                foreach (var geoJsonId in dto.MunicipalityGeoJsonIds)
                {
                    if (
                        municipalities.TryGetValue(geoJsonId, out var municipalityId)
                        && !existingLinks.Contains(municipalityId)
                    )
                    {
                        context.MunicipalitySpecies.Add(
                            new MunicipalitySpecies
                            {
                                Id = Guid.CreateVersion7(),
                                MunicipalityId = municipalityId,
                                SpeciesId = species.Id,
                            }
                        );
                    }
                }
            }

            // Link to categories
            if (dto.CategoryCodes is { Count: > 0 })
            {
                var existingCategoryLinks = await context
                    .SpeciesCategoryLinks.Where(scl => scl.SpeciesId == species.Id)
                    .Select(scl => scl.CategoryId)
                    .ToHashSetAsync(stoppingToken);

                foreach (var categoryCode in dto.CategoryCodes)
                {
                    if (
                        categories.TryGetValue(categoryCode, out var category)
                        && !existingCategoryLinks.Contains(category.Id)
                    )
                    {
                        context.SpeciesCategoryLinks.Add(
                            new SpeciesCategoryLink
                            {
                                Id = Guid.CreateVersion7(),
                                SpeciesId = species.Id,
                                CategoryId = category.Id,
                            }
                        );
                    }
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }

        logger.LogInformation(
            "Species seeded: {Count} new, {Total} total",
            seededCount,
            speciesList.Count
        );
    }

    private async Task SeedFwsLinksAsync(WildlifeDbContext context, CancellationToken stoppingToken)
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "fws_links.json");
        if (!File.Exists(jsonPath))
        {
            logger.LogWarning("fws_links.json not found. Skipping FWS links seeding.");
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath, stoppingToken);
        var links = JsonSerializer.Deserialize<List<FwsLinkDto>>(json, JsonOptions);
        if (links is null)
            return;

        var speciesMap = await context.Species.ToDictionaryAsync(
            s => s.ScientificName,
            s => s.Id,
            stoppingToken
        );

        var practiceMap = await context.NrcsPractices.ToDictionaryAsync(
            p => p.Code,
            p => p.Id,
            stoppingToken
        );

        var actionMap = await context.FwsActions.ToDictionaryAsync(
            a => a.Code,
            a => a.Id,
            stoppingToken
        );

        var existingLinks = await context
            .FwsLinks.Select(l => new
            {
                l.SpeciesId,
                l.NrcsPracticeId,
                l.FwsActionId,
            })
            .ToListAsync(stoppingToken);

        var existingLinkSet = existingLinks
            .Select(l => (l.SpeciesId, l.NrcsPracticeId, l.FwsActionId))
            .ToHashSet();

        var batchCount = 0;
        var seededCount = 0;
        foreach (var dto in links)
        {
            if (!speciesMap.TryGetValue(dto.SpeciesScientificName, out var speciesId))
                continue;

            if (!practiceMap.TryGetValue(dto.NrcsPracticeCode, out var practiceId))
                continue;

            if (!actionMap.TryGetValue(dto.FwsActionCode, out var actionId))
                continue;

            var linkKey = (speciesId, practiceId, actionId);
            if (existingLinkSet.Contains(linkKey))
                continue;

            context.FwsLinks.Add(
                new FwsLink
                {
                    Id = Guid.CreateVersion7(),
                    SpeciesId = speciesId,
                    NrcsPracticeId = practiceId,
                    FwsActionId = actionId,
                    Justification = dto.Justification,
                }
            );

            existingLinkSet.Add(linkKey);
            batchCount++;
            seededCount++;

            if (batchCount >= 100)
            {
                await context.SaveChangesAsync(stoppingToken);
                batchCount = 0;
            }
        }

        if (batchCount > 0)
        {
            await context.SaveChangesAsync(stoppingToken);
        }

        logger.LogInformation("FWS links seeded: {Count}", seededCount);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    #region DTOs for JSON deserialization

    private sealed class NrcsPracticeDto
    {
        [JsonPropertyName("code")]
        public required string Code { get; init; }

        [JsonPropertyName("name")]
        public required List<LocaleValue> Name { get; init; }
    }

    private sealed class FwsActionDto
    {
        [JsonPropertyName("code")]
        public required string Code { get; init; }

        [JsonPropertyName("name")]
        public required List<LocaleValue> Name { get; init; }
    }

    private sealed class SpeciesDto
    {
        [JsonPropertyName("scientificName")]
        public required string ScientificName { get; init; }

        [JsonPropertyName("commonNameEn")]
        public required string CommonNameEn { get; init; }

        [JsonPropertyName("commonNameEs")]
        public required string CommonNameEs { get; init; }

        [JsonPropertyName("municipalityGeoJsonIds")]
        public List<string>? MunicipalityGeoJsonIds { get; init; }

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; init; }

        [JsonPropertyName("imageContentType")]
        public string? ImageContentType { get; init; }

        [JsonPropertyName("imageSourceUrl")]
        public string? ImageSourceUrl { get; init; }

        [JsonPropertyName("categoryCodes")]
        public List<string>? CategoryCodes { get; init; }

        [JsonPropertyName("isFauna")]
        public bool IsFauna { get; init; } = true;

        [JsonPropertyName("elCode")]
        public string? ElCode { get; init; }

        [JsonPropertyName("gRank")]
        public string? GRank { get; init; }

        [JsonPropertyName("sRank")]
        public string? SRank { get; init; }
    }

    private sealed class FwsLinkDto
    {
        [JsonPropertyName("speciesScientificName")]
        public required string SpeciesScientificName { get; init; }

        [JsonPropertyName("nrcsPracticeCode")]
        public required string NrcsPracticeCode { get; init; }

        [JsonPropertyName("fwsActionCode")]
        public required string FwsActionCode { get; init; }

        [JsonPropertyName("justification")]
        public List<LocaleValue> Justification { get; init; } = [];
    }

    #endregion
}
