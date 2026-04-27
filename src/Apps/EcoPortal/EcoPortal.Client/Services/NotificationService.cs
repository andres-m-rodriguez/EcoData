using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

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
        try
        {
            _unreadCount = await _notificationClient.GetUnreadCountAsync();
            OnUnreadCountChanged?.Invoke();
        }
        catch
        {
            // Silently fail — user might not be authenticated.
        }
    }

    public Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(
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

    public async Task MarkAllAsReadAsync()
    {
        await _notificationClient.MarkAllAsReadAsync();
        _unreadCount = 0;
        OnUnreadCountChanged?.Invoke();
    }

    // OnNotificationReceived is preserved for compatibility with NotificationPanel,
    // but no longer fires automatically — live push goes away with the SSE removal
    // and comes back when the Service Bus hybrid bridge ships (issue #224 / #222).
    internal void RaiseNotificationReceived() => OnNotificationReceived?.Invoke();
}
