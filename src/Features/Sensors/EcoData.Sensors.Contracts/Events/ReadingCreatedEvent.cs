namespace EcoData.Sensors.Contracts.Events;

/// <summary>
/// Event published when a new sensor reading is created.
/// </summary>
public sealed record ReadingCreatedEvent(
    Guid SensorId,
    string Parameter,
    string? Description,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
);
