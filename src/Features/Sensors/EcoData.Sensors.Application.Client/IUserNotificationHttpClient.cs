using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface IUserNotificationHttpClient
{
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

    string GetStreamUrl();
}
