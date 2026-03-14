namespace EcoData.Sensors.Contracts.Dtos;

/// <summary>
/// Represents a water quality sensor reading from an IoT device.
/// </summary>
public sealed record SensorReadingDto(
    double? Temperature = null,
    double? Ph = null,
    double? DissolvedOxygen = null,
    double? Turbidity = null,
    double? Conductivity = null,
    DateTimeOffset? RecordedAt = null
);
