namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record ReadingDtoForDetail(
    Guid Id,
    Guid SensorId,
    string Parameter,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt,
    DateTimeOffset IngestedAt
);

public sealed record ReadingDtoForList(
    Guid Id,
    Guid SensorId,
    string SensorName,
    string Parameter,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);

public sealed record ReadingDtoForCreate(
    Guid SensorId,
    string Parameter,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);
