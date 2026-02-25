namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record SensorForListDto(
    Guid Id,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string? Municipality,
    bool IsActive
);

public sealed record SensorForDetailDto(
    Guid Id,
    Guid SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string? Municipality,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string DataSourceName
);
