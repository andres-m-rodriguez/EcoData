namespace EcoData.Sensors.Contracts.Events;

/// <summary>
/// Event published when a user notification is created.
/// </summary>
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
    /// <summary>Service Bus subscription name. Must match <c>typeof(UserNotificationEvent).Name.ToLowerInvariant()</c>.</summary>
    public const string SubscriptionName = "usernotificationevent";
}
