namespace EcoData.Sensors.Contracts.Dtos;

public sealed record ReadingBatchDtoForCreate(
    Guid SensorId,
    IReadOnlyList<ReadingItemDto> Readings
);

public sealed record ReadingItemDto(
    string Parameter,
    string? Description,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);

public sealed record ReadingBatchResult(
    int TotalSubmitted,
    int Accepted,
    int Rejected,
    IReadOnlyList<string> Errors
);
