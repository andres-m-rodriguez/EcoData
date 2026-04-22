using EcoData.Common.Messaging.Abstractions;
using EcoData.Sensors.Contracts;
using EcoData.Sensors.Contracts.Events;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database.Models;
using Microsoft.Extensions.Logging;

namespace EcoPortal.Server.Services;

public sealed class NotificationRoutingService(
    IUserSensorSubscriptionRepository subscriptionRepository,
    IUserNotificationRepository notificationRepository,
    IMessageBus messageBus,
    ILogger<NotificationRoutingService> logger
) : INotificationRoutingService
{
    public async Task RouteHealthAlertAsync(
        SensorHealthAlertEvent alertEvent,
        CancellationToken cancellationToken = default)
    {
        var (isStale, isUnhealthy, isRecovered) = alertEvent.AlertType switch
        {
            "Stale" => (true, false, false),
            "Unhealthy" => (false, true, false),
            "Recovered" => (false, false, true),
            _ => (false, false, false)
        };

        if (!isStale && !isUnhealthy && !isRecovered)
        {
            logger.LogWarning(
                "Unknown alert type {AlertType} for alert {AlertId}",
                alertEvent.AlertType,
                alertEvent.Id
            );
            return;
        }

        var subscribedUserIds = await subscriptionRepository.GetSubscribedUserIdsAsync(
            alertEvent.SensorId,
            isStale,
            isUnhealthy,
            isRecovered,
            cancellationToken
        );

        if (subscribedUserIds.Count == 0)
        {
            logger.LogDebug(
                "No users subscribed to {AlertType} alerts for sensor {SensorId}",
                alertEvent.AlertType,
                alertEvent.SensorId
            );
            return;
        }

        logger.LogInformation(
            "Routing {AlertType} alert for sensor {SensorId} ({SensorName}) to {UserCount} users",
            alertEvent.AlertType,
            alertEvent.SensorId,
            alertEvent.SensorName,
            subscribedUserIds.Count
        );

        var notificationType = alertEvent.AlertType switch
        {
            "Stale" => NotificationType.SensorStale,
            "Unhealthy" => NotificationType.SensorUnhealthy,
            "Recovered" => NotificationType.SensorRecovered,
            _ => throw new InvalidOperationException($"Unknown alert type: {alertEvent.AlertType}")
        };

        var title = alertEvent.AlertType switch
        {
            "Stale" => $"Sensor Stale: {alertEvent.SensorName}",
            "Unhealthy" => $"Sensor Unhealthy: {alertEvent.SensorName}",
            "Recovered" => $"Sensor Recovered: {alertEvent.SensorName}",
            _ => $"Sensor Alert: {alertEvent.SensorName}"
        };

        var notifications = subscribedUserIds.Select(userId => (
            UserId: userId,
            SensorId: alertEvent.SensorId,
            AlertId: (Guid?)alertEvent.Id,
            Title: title,
            Message: alertEvent.Message,
            Type: notificationType
        ));

        var createdNotifications = await notificationRepository.CreateManyAsync(
            notifications,
            cancellationToken
        );

        foreach (var notification in createdNotifications)
        {
            var userEvent = new UserNotificationEvent(
                notification.Id,
                subscribedUserIds.First(u =>
                    createdNotifications.Any(n => n.Id == notification.Id)),
                notification.SensorId,
                notification.SensorName,
                notification.AlertId,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt
            );

            // Find the userId for this notification
            var userId = subscribedUserIds[createdNotifications.ToList().IndexOf(notification)];

            var correctedEvent = new UserNotificationEvent(
                notification.Id,
                userId,
                notification.SensorId,
                notification.SensorName,
                notification.AlertId,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt
            );

            await messageBus.PublishEventAsync(
                correctedEvent,
                MessageTopics.GetUserNotificationsTopic(userId),
                cancellationToken: cancellationToken
            );

            logger.LogDebug(
                "Published notification {NotificationId} to user {UserId}",
                notification.Id,
                userId
            );
        }

        logger.LogInformation(
            "Created and dispatched {NotificationCount} notifications for alert {AlertId}",
            createdNotifications.Count,
            alertEvent.Id
        );
    }
}
