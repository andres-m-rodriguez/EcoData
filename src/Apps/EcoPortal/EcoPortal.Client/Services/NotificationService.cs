using System.Net.Http.Json;
using System.Text.Json;
using EcoData.Sensors.Application.Client;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Events;
using Microsoft.JSInterop;
using MudBlazor;

namespace EcoPortal.Client.Services;

public sealed class NotificationService : IAsyncDisposable
{
    private readonly IUserNotificationHttpClient _notificationClient;
    private readonly ISnackbar _snackbar;
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _sseTokenSource;
    private Task? _sseTask;
    private int _unreadCount;
    private bool _isConnected;
    private bool _isDisposed;

    public int UnreadCount => _unreadCount;
    public bool IsConnected => _isConnected;

    public event Action? OnNotificationReceived;
    public event Action? OnUnreadCountChanged;
    public event Action? OnConnectionStateChanged;

    public NotificationService(
        IUserNotificationHttpClient notificationClient,
        ISnackbar snackbar,
        IJSRuntime jsRuntime,
        HttpClient httpClient)
    {
        _notificationClient = notificationClient;
        _snackbar = snackbar;
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        if (_isDisposed) return;

        await RefreshUnreadCountAsync();
        await StartSseConnectionAsync();
    }

    public async Task RefreshUnreadCountAsync()
    {
        if (_isDisposed) return;

        try
        {
            _unreadCount = await _notificationClient.GetUnreadCountAsync();
            OnUnreadCountChanged?.Invoke();
        }
        catch
        {
            // Silently fail - user might not be authenticated
        }
    }

    public async Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null)
    {
        return await _notificationClient.GetNotificationsAsync(pageSize, cursor);
    }

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
        var count = await _notificationClient.MarkAllAsReadAsync();
        _unreadCount = 0;
        OnUnreadCountChanged?.Invoke();
    }

    public void Stop()
    {
        _sseTokenSource?.Cancel();
        _sseTokenSource?.Dispose();
        _sseTokenSource = null;
        _isConnected = false;
        OnConnectionStateChanged?.Invoke();
    }

    private async Task StartSseConnectionAsync()
    {
        if (_isDisposed) return;

        _sseTokenSource?.Cancel();
        _sseTokenSource?.Dispose();
        _sseTokenSource = new CancellationTokenSource();

        var token = _sseTokenSource.Token;
        var streamUrl = _notificationClient.GetStreamUrl();

        _sseTask = Task.Run(async () =>
        {
            var reconnectDelay = TimeSpan.FromSeconds(1);
            var maxDelay = TimeSpan.FromSeconds(30);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
                    request.Headers.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
                    );

                    using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        token
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        await Task.Delay(reconnectDelay, token);
                        reconnectDelay = TimeSpan.FromSeconds(
                            Math.Min(reconnectDelay.TotalSeconds * 2, maxDelay.TotalSeconds)
                        );
                        continue;
                    }

                    _isConnected = true;
                    OnConnectionStateChanged?.Invoke();
                    reconnectDelay = TimeSpan.FromSeconds(1);

                    using var stream = await response.Content.ReadAsStreamAsync(token);
                    using var reader = new StreamReader(stream);

                    string? eventType = null;
                    string dataBuffer = "";

                    while (!token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(token);

                        if (line == null)
                        {
                            // Stream ended
                            break;
                        }

                        if (string.IsNullOrEmpty(line))
                        {
                            // Empty line = end of event
                            if (!string.IsNullOrEmpty(dataBuffer))
                            {
                                await ProcessEventAsync(eventType, dataBuffer);
                            }
                            eventType = null;
                            dataBuffer = "";
                            continue;
                        }

                        if (line.StartsWith("event:"))
                        {
                            eventType = line[6..].Trim();
                        }
                        else if (line.StartsWith("data:"))
                        {
                            var data = line[5..].Trim();
                            dataBuffer += data;
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    _isConnected = false;
                    OnConnectionStateChanged?.Invoke();

                    if (!token.IsCancellationRequested)
                    {
                        await Task.Delay(reconnectDelay, token);
                        reconnectDelay = TimeSpan.FromSeconds(
                            Math.Min(reconnectDelay.TotalSeconds * 2, maxDelay.TotalSeconds)
                        );
                    }
                }
            }

            _isConnected = false;
            OnConnectionStateChanged?.Invoke();
        }, token);
    }

    private async Task ProcessEventAsync(string? eventType, string data)
    {
        if (eventType != "user.notification") return;

        try
        {
            var notification = JsonSerializer.Deserialize<UserNotificationEvent>(
                data,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (notification is null) return;

            _unreadCount++;
            OnUnreadCountChanged?.Invoke();
            OnNotificationReceived?.Invoke();

            // Show snackbar
            _snackbar.Add(
                notification.Title,
                Severity.Info,
                config =>
                {
                    config.VisibleStateDuration = 5000;
                    config.ShowCloseIcon = true;
                    config.Icon = notification.Type switch
                    {
                        "SensorStale" => Icons.Material.Filled.Warning,
                        "SensorUnhealthy" => Icons.Material.Filled.Error,
                        "SensorRecovered" => Icons.Material.Filled.CheckCircle,
                        _ => Icons.Material.Filled.Notifications
                    };
                }
            );
        }
        catch
        {
            // Ignore parsing errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();

        if (_sseTask is not null)
        {
            try
            {
                await _sseTask;
            }
            catch
            {
                // Ignore task cancellation exceptions
            }
        }
    }
}
