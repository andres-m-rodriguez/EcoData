using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface IUserNotificationHttpClient
{
    Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null,
        string? sensorName = null,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<UserNotificationDto> GetNotificationsAsync(
        NotificationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<int> GetUnreadCountAsync(
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserNotificationDto, ProblemDetail>> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default
    );

    Task<int> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default
    );
}
