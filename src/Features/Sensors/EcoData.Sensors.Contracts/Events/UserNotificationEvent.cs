namespace EcoData.Sensors.Contracts.Events;

public sealed record UserNotificationEvent(
    Guid Id,
    Guid UserId,
    Guid SensorId,
    string SensorName,
    Guid? AlertId,
    string Title,
    string Message,
    string Type,
    DateTimeOffset CreatedAt
)
{
    // Service Bus subscription name. Must match typeof(UserNotificationEvent).Name.ToLowerInvariant().
    public const string SubscriptionName = "usernotificationevent";
}
