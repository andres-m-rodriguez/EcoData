using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface IUserNotificationHttpClient
{
    Task<OneOf<IReadOnlyList<UserNotificationDto>, RequestFailed>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null,
        string? sensorName = null,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<UserNotificationDto> GetNotificationsAsync(
        NotificationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<int, RequestFailed>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserNotificationDto, RequestFailed>> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<int, RequestFailed>> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default
    );
}
