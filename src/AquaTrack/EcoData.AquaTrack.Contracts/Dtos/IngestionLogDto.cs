namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record IngestionLogDtoForDetail(
    Guid Id,
    Guid DataSourceId,
    DateTimeOffset IngestedAt,
    int RecordCount,
    DateTimeOffset LastRecordedAt
);

public sealed record IngestionLogDtoForCreate(
    Guid DataSourceId,
    int RecordCount,
    DateTimeOffset LastRecordedAt
);
