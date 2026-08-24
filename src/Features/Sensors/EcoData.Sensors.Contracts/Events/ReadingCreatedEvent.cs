namespace EcoData.Sensors.Contracts.Events;

public sealed record ReadingCreatedEvent(
    Guid SensorId,
    string Parameter,
    string? Description,
    double Value,
    string Unit,
    DateTimeOffset RecordedAt
)
{
    // Service Bus subscription name. Must match typeof(ReadingCreatedEvent).Name.ToLowerInvariant().
    public const string SubscriptionName = "readingcreatedevent";
}
