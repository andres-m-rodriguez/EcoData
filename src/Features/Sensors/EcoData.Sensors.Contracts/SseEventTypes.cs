namespace EcoData.Sensors.Contracts;

/// <summary>
/// Server-Sent Events event type constants for the Sensors module.
/// Use these when publishing or subscribing to SSE streams.
/// </summary>
public static class SseEventTypes
{
    /// <summary>
    /// Event type for new sensor readings.
    /// Payload: <see cref="Dtos.ReadingDto"/>
    /// </summary>
    public const string Reading = "sensor.reading";

    /// <summary>
    /// Event type for sensor health status changes.
    /// Payload: <see cref="Dtos.SensorHealthDto"/>
    /// </summary>
    public const string HealthChanged = "sensor.health.changed";

    /// <summary>
    /// Event type for sensor health alerts (stale, unhealthy, recovered).
    /// </summary>
    public const string HealthAlert = "sensor.health.alert";
}
