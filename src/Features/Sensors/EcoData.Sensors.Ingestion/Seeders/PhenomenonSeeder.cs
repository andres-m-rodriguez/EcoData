using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.Sensors.Ingestion.Seeders;

public sealed class PhenomenonSeeder(
    IDbContextFactory<SensorsDbContext> contextFactory,
    IDataSourceRepository dataSourceRepository,
    ILogger<PhenomenonSeeder> logger
) : IHostedService
{
    private const string UsgsDataSourceName = "USGS Puerto Rico";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedPhenomenaAsync(cancellationToken);
        await SeedUsgsParameterMappingsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedPhenomenaAsync(CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var existingCodes = await context
            .Phenomena.Select(p => p.Code)
            .ToListAsync(ct);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        var toAdd = PhenomenonCatalog
            .All.Where(p => !existingSet.Contains(p.Code))
            .Select(p => new Phenomenon
            {
                Id = Guid.CreateVersion7(),
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                CanonicalUnit = p.CanonicalUnit,
                DefaultValueShape = p.DefaultValueShape,
                Capabilities = p.Capabilities,
                CreatedAt = now,
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            logger.LogDebug("All {Total} phenomena already seeded", PhenomenonCatalog.All.Count);
            return;
        }

        context.Phenomena.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} phenomena", toAdd.Count);
    }

    private async Task SeedUsgsParameterMappingsAsync(CancellationToken ct)
    {
        var dataSource = await dataSourceRepository.GetByNameAsync(UsgsDataSourceName, ct);
        if (dataSource is null)
        {
            logger.LogWarning(
                "USGS data source '{Name}' not found; skipping USGS parameter mapping seed. Mappings will be seeded on next worker run after the data source is created.",
                UsgsDataSourceName
            );
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var phenomenaByCode = await context
            .Phenomena.ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);

        var existingCodes = await context
            .Parameters.Where(p => p.SourceId == dataSource.Id)
            .Select(p => p.Code)
            .ToListAsync(ct);

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        var toAdd = new List<Parameter>();
        foreach (var mapping in UsgsParameterMappings.All)
        {
            if (existingSet.Contains(mapping.Code))
            {
                continue;
            }
            if (!phenomenaByCode.TryGetValue(mapping.PhenomenonCode, out var phenomenonId))
            {
                logger.LogWarning(
                    "USGS code {Code} maps to phenomenon '{PhenomenonCode}' which is not in the catalog; skipping",
                    mapping.Code,
                    mapping.PhenomenonCode
                );
                continue;
            }

            toAdd.Add(new Parameter
            {
                Id = Guid.CreateVersion7(),
                SourceId = dataSource.Id,
                Code = mapping.Code,
                Name = mapping.Name,
                DefaultUnit = mapping.SourceUnit,
                SensorTypeId = null,
                PhenomenonId = phenomenonId,
                SourceUnit = mapping.SourceUnit,
                UnitFactor = mapping.UnitFactor,
                UnitOffset = mapping.UnitOffset,
                ValueShape = mapping.ValueShape,
                CreatedAt = now,
            });
        }

        if (toAdd.Count == 0)
        {
            logger.LogDebug("All {Total} USGS parameter mappings already seeded", UsgsParameterMappings.All.Count);
            return;
        }

        context.Parameters.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} USGS parameter mappings", toAdd.Count);
    }
}

internal sealed record PhenomenonSeed(
    string Code,
    string Name,
    string? Description,
    string CanonicalUnit,
    ValueShape DefaultValueShape,
    string[] Capabilities
);

internal static class PhenomenonCatalog
{
    public static readonly IReadOnlyList<PhenomenonSeed> All =
    [
        new("streamflow", "Streamflow", "Volumetric water flow rate in a stream channel", "m3/s", ValueShape.Instantaneous, ["FloodIndicator"]),
        new("gage-height", "Gage Height", "Height of the water surface above a fixed reference at the gage", "m", ValueShape.Instantaneous, ["FloodIndicator"]),
        new("precipitation", "Precipitation", "Liquid precipitation accumulated over a reporting interval", "mm", ValueShape.IntervalTotal, ["FloodIndicator"]),
        new("water-level-prd2002", "Stream Water Level (PRD2002)", "Water surface elevation above the Puerto Rico Datum of 2002", "m", ValueShape.Instantaneous, ["FloodIndicator"]),
        new("reservoir-level-lmsl", "Reservoir Level (LMSL)", "Reservoir surface elevation above local mean sea level", "m", ValueShape.Instantaneous, ["FloodIndicator", "DroughtIndicator"]),
        new("reservoir-level-prd2002", "Reservoir Level (PRD2002)", "Reservoir surface elevation above the Puerto Rico Datum of 2002", "m", ValueShape.Instantaneous, ["FloodIndicator", "DroughtIndicator"]),
        new("groundwater-depth", "Groundwater Depth", "Depth from land surface down to the water table", "m", ValueShape.Instantaneous, ["DroughtIndicator"]),
        new("water-temperature", "Water Temperature", "Temperature of the water body", "degC", ValueShape.Instantaneous, ["WaterQualityIndicator"]),
        new("ph", "pH", "Acidity or alkalinity of the water on the standard pH scale", "pH", ValueShape.Instantaneous, ["WaterQualityIndicator"]),
        new("dissolved-oxygen", "Dissolved Oxygen", "Concentration of oxygen dissolved in water", "mg/L", ValueShape.Instantaneous, ["WaterQualityIndicator"]),
        new("specific-conductance", "Specific Conductance", "Electrical conductivity of water normalized to 25 degC", "uS/cm", ValueShape.Instantaneous, ["WaterQualityIndicator"]),
        new("turbidity", "Turbidity", "Cloudiness of water caused by suspended particles", "FNU", ValueShape.Instantaneous, ["WaterQualityIndicator"]),
        new("gate-opening-height", "Gate Opening Height", "Vertical opening of a control gate", "m", ValueShape.Instantaneous, []),
    ];
}

internal sealed record UsgsParameterMapping(
    string Code,
    string Name,
    string PhenomenonCode,
    string SourceUnit,
    double UnitFactor,
    double UnitOffset,
    ValueShape ValueShape
);

internal static class UsgsParameterMappings
{
    private const double FeetToMeters = 0.3048;
    private const double CubicFeetPerSecondToCubicMetersPerSecond = 0.0283168;
    private const double InchesToMillimeters = 25.4;

    public static readonly IReadOnlyList<UsgsParameterMapping> All =
    [
        new("00010", "Temperature, water", "water-temperature", "deg C", 1.0, 0.0, ValueShape.Instantaneous),
        new("00045", "Precipitation, total", "precipitation", "in", InchesToMillimeters, 0.0, ValueShape.IntervalTotal),
        new("00060", "Streamflow", "streamflow", "ft3/s", CubicFeetPerSecondToCubicMetersPerSecond, 0.0, ValueShape.Instantaneous),
        new("00065", "Gage height", "gage-height", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("00095", "Specific conductance", "specific-conductance", "uS/cm @25C", 1.0, 0.0, ValueShape.Instantaneous),
        new("00300", "Dissolved oxygen", "dissolved-oxygen", "mg/l", 1.0, 0.0, ValueShape.Instantaneous),
        new("00400", "pH, water, field", "ph", "std units", 1.0, 0.0, ValueShape.Instantaneous),
        new("45592", "Gate opening height", "gate-opening-height", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("63680", "Turbidity (near-IR, FNU)", "turbidity", "FNU", 1.0, 0.0, ValueShape.Instantaneous),
        new("72019", "Depth to water level below land surface", "groundwater-depth", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("72365", "Stream water level elevation (PRD2002)", "water-level-prd2002", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("72375", "Reservoir elevation (LMSL, ft)", "reservoir-level-lmsl", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("72376", "Reservoir elevation (LMSL, m)", "reservoir-level-lmsl", "m", 1.0, 0.0, ValueShape.Instantaneous),
        new("72379", "Reservoir elevation (PRD2002, ft)", "reservoir-level-prd2002", "ft", FeetToMeters, 0.0, ValueShape.Instantaneous),
        new("72380", "Reservoir elevation (PRD2002, m)", "reservoir-level-prd2002", "m", 1.0, 0.0, ValueShape.Instantaneous),
    ];
}
