namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorHealthStatusDtoForDetail(
    Guid Id,
    Guid SensorId,
    string SensorName,
    DateTimeOffset? LastReadingAt,
    DateTimeOffset? LastHeartbeatAt,
    string Status,
    int ConsecutiveFailures,
    string? LastErrorMessage,
    DateTimeOffset UpdatedAt
);

public sealed record SensorHealthStatusDtoForList(
    Guid SensorId,
    string SensorName,
    string Status,
    DateTimeOffset? LastReadingAt,
    int ConsecutiveFailures
);

public sealed record SensorHealthConfigDtoForDetail(
    Guid Id,
    Guid SensorId,
    int ExpectedIntervalSeconds,
    int StaleThresholdSeconds,
    int UnhealthyThresholdSeconds,
    bool IsMonitoringEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record SensorHealthConfigDtoForCreate(
    int ExpectedIntervalSeconds,
    int StaleThresholdSeconds,
    int UnhealthyThresholdSeconds,
    bool IsMonitoringEnabled
);

public sealed record SensorHealthConfigDtoForUpdate(
    int ExpectedIntervalSeconds,
    int StaleThresholdSeconds,
    int UnhealthyThresholdSeconds,
    bool IsMonitoringEnabled
);

public sealed record SensorHealthAlertDtoForList(
    Guid Id,
    Guid SensorId,
    string SensorName,
    string AlertType,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ResolvedAt,
    string Message
);

public sealed record SensorHealthSummaryDto(
    int TotalMonitored,
    int Healthy,
    int Stale,
    int Unhealthy,
    int Unknown
);
