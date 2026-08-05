using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoPortal.Client.Services;

public sealed class NotificationService
{
    private readonly IUserNotificationHttpClient _notificationClient;
    private int _unreadCount;

    public int UnreadCount => _unreadCount;

    public event Action? OnNotificationReceived;
    public event Action? OnUnreadCountChanged;

    public NotificationService(IUserNotificationHttpClient notificationClient)
    {
        _notificationClient = notificationClient;
    }

    public async Task InitializeAsync()
    {
        await RefreshUnreadCountAsync();
    }

    public async Task RefreshUnreadCountAsync()
    {
        // Clients never throw; on failure (e.g. user not authenticated) keep the count unchanged.
        var result = await _notificationClient.GetUnreadCountAsync();
        if (result.TryPickT0(out var count, out _))
        {
            _unreadCount = count;
            OnUnreadCountChanged?.Invoke();
        }
    }

    public Task<OneOf<IReadOnlyList<UserNotificationDto>, RequestFailed>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null,
        string? sensorName = null)
        => _notificationClient.GetNotificationsAsync(pageSize, cursor, sensorName);

    public IAsyncEnumerable<UserNotificationDto> GetNotificationsAsync(
        NotificationParameters parameters,
        CancellationToken cancellationToken = default)
        => _notificationClient.GetNotificationsAsync(parameters, cancellationToken);

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var result = await _notificationClient.MarkAsReadAsync(notificationId);
        if (result.IsT0)
        {
            _unreadCount = Math.Max(0, _unreadCount - 1);
            OnUnreadCountChanged?.Invoke();
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        var result = await _notificationClient.MarkAllAsReadAsync();
        if (result.IsT1)
        {
            return false;
        }

        _unreadCount = 0;
        OnUnreadCountChanged?.Invoke();
        return true;
    }

    // OnNotificationReceived is preserved for compatibility with NotificationPanel,
    // but no longer fires automatically — live push goes away with the SSE removal
    // and comes back when the Service Bus hybrid bridge ships (issue #224 / #222).
    internal void RaiseNotificationReceived() => OnNotificationReceived?.Invoke();
}
