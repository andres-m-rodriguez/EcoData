namespace EcoData.Sensors.Contracts;

public static class SseEventTypes
{
    public const string Reading = "sensor.reading";

    public const string HealthChanged = "sensor.health.changed";

    public const string HealthAlert = "sensor.health.alert";

    public const string UserNotification = "user.notification";
}

public static class MessageTopics
{
    public const string AllHealthAlerts = "all-health-alerts";

    public const string UserNotificationsPrefix = "user-notifications";

    public static string GetUserNotificationsTopic(Guid userId) =>
        $"{UserNotificationsPrefix}:{userId}";
}
