namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record ReadingDto(
    Guid Id,
    Guid SensorId,
    string Parameter,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt,
    DateTimeOffset IngestedAt
);

public sealed record ReadingWithSensorDto(
    Guid Id,
    Guid SensorId,
    string SensorName,
    string Parameter,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);
