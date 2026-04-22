using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Database.Models;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IUserNotificationRepository
{
    IAsyncEnumerable<UserNotificationDto> GetByUserAsync(
        Guid userId,
        NotificationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<UserNotificationDto?> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default
    );

    Task<int> MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<UserNotificationDto> CreateAsync(
        Guid userId,
        Guid sensorId,
        Guid? alertId,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<UserNotificationDto>> CreateManyAsync(
        IEnumerable<(Guid UserId, Guid SensorId, Guid? AlertId, string Title, string Message, NotificationType Type)> notifications,
        CancellationToken cancellationToken = default
    );
}
