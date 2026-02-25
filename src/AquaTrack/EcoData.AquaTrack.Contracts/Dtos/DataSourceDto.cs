namespace EcoData.AquaTrack.Contracts.Dtos;

public sealed record DataSourceDto(
    Guid Id,
    string Name,
    string Type,
    string? BaseUrl,
    int PullIntervalSeconds,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int SensorCount
);
