namespace EcoData.Sensors.Contracts.Events;

public sealed record SensorHealthAlertEvent(
    Guid Id,
    Guid SensorId,
    string SensorName,
    string AlertType,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ResolvedAt,
    string Message
)
{
    // Service Bus subscription name. Must match typeof(SensorHealthAlertEvent).Name.ToLowerInvariant().
    public const string SubscriptionName = "sensorhealthalertevent";
}
