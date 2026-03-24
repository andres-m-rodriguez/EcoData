namespace EcoData.Sensors.Contracts.Events;

/// <summary>
/// Event published when a sensor health alert is triggered.
/// </summary>
public sealed record SensorHealthAlertEvent(
    Guid Id,
    Guid SensorId,
    string SensorName,
    string AlertType,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ResolvedAt,
    string Message
);
