namespace EcoData.Sensors.Contracts.Dtos;

public sealed record UserSensorSubscriptionDto(
    Guid Id,
    Guid SensorId,
    string SensorName,
    bool NotifyOnStale,
    bool NotifyOnUnhealthy,
    bool NotifyOnRecovered,
    DateTimeOffset CreatedAt
);

public sealed record UserSensorSubscriptionDtoForCreate(
    bool NotifyOnStale,
    bool NotifyOnUnhealthy,
    bool NotifyOnRecovered
);

public sealed record UserSensorSubscriptionDtoForUpdate(
    bool NotifyOnStale,
    bool NotifyOnUnhealthy,
    bool NotifyOnRecovered
);
