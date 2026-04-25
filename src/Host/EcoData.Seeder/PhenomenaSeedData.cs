using EcoData.Sensors.Database.Models;

namespace EcoData.Seeder;

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
