namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record SensorDtoForList(
    Guid Id,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string? Municipality,
    bool IsActive
);

public sealed record SensorDtoForDetail(
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

public sealed record SensorDtoForCreate(
    Guid SourceId,
    string ExternalId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string? Municipality,
    bool IsActive
);

public sealed record SensorDtoForCreated(
    Guid Id,
    string ExternalId
);
