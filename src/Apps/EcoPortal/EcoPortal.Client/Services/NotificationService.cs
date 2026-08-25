using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoPortal.Client.Features.Notifications.Components;
using OneOf;
using Tempest;

namespace EcoPortal.Client.Services;

public sealed class NotificationService(IUserNotificationHttpClient notificationClient, IEventBus bus)
{
    private int _unreadCount;

    public int UnreadCount => _unreadCount;

    public async Task InitializeAsync()
    {
        await RefreshUnreadCountAsync();
    }

    public async Task RefreshUnreadCountAsync()
    {
        // Clients never throw; on failure (e.g. user not authenticated) keep the count unchanged.
        var result = await notificationClient.GetUnreadCountAsync();
        if (result.TryPickT0(out var count, out _))
        {
            _unreadCount = count;
            bus.Publish(new NotificationBell.UnreadCountChanged(_unreadCount));
        }
    }

    public Task<OneOf<IReadOnlyList<UserNotificationDto>, RequestFailed>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null,
        string? sensorName = null)
        => notificationClient.GetNotificationsAsync(pageSize, cursor, sensorName);

    public IAsyncEnumerable<UserNotificationDto> GetNotificationsAsync(
        NotificationParameters parameters,
        CancellationToken cancellationToken = default)
        => notificationClient.GetNotificationsAsync(parameters, cancellationToken);

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var result = await notificationClient.MarkAsReadAsync(notificationId);
        if (result.IsT0)
        {
            _unreadCount = Math.Max(0, _unreadCount - 1);
            bus.Publish(new NotificationBell.UnreadCountChanged(_unreadCount));
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        var result = await notificationClient.MarkAllAsReadAsync();
        if (result.IsT1)
        {
            return false;
        }

        _unreadCount = 0;
        bus.Publish(new NotificationBell.UnreadCountChanged(_unreadCount));
        return true;
    }
}
