namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record DataSourceDtoForList(
    Guid Id,
    string Name,
    string Type,
    string? BaseUrl,
    int PullIntervalSeconds,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int SensorCount
);

public sealed record DataSourceDtoForCreate(
    string Name,
    string Type,
    string? BaseUrl,
    string? ApiKey,
    int PullIntervalSeconds,
    bool IsActive
);

public sealed record DataSourceDtoForCreated(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt
);
