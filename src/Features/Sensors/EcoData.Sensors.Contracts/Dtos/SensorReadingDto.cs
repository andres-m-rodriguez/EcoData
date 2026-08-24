namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorReadingDto(
    double? Temperature = null,
    double? Ph = null,
    double? DissolvedOxygen = null,
    double? Turbidity = null,
    double? Conductivity = null,
    DateTimeOffset? RecordedAt = null
);
