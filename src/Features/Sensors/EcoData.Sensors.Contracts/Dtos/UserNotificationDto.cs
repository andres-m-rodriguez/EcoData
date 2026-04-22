namespace EcoData.Sensors.Contracts.Dtos;

public sealed record UserNotificationDto(
    Guid Id,
    Guid SensorId,
    string SensorName,
    Guid? AlertId,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
);

public sealed record UnreadCountDto(int Count);
