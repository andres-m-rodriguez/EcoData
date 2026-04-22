using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class UserNotificationHttpClient(HttpClient httpClient) : IUserNotificationHttpClient
{
    public async Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(
        int pageSize = 20,
        Guid? cursor = null,
        string? sensorName = null,
        CancellationToken cancellationToken = default)
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", pageSize != 20 ? pageSize : null)
            .Add("cursor", cursor)
            .Add("sensorName", sensorName)
            .Build();

        var response = await httpClient.GetAsync(
            $"users/me/notifications{queryString}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<UserNotificationDto>>(
            cancellationToken
        );
        return result ?? [];
    }

    public IAsyncEnumerable<UserNotificationDto> GetNotificationsAsync(
        NotificationParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("sensorName", parameters.SensorName)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<UserNotificationDto>(
            $"users/me/notifications{queryString}",
            cancellationToken
        )!;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<UnreadCountDto>(
            "users/me/notifications/unread-count",
            cancellationToken
        );
        return result?.Count ?? 0;
    }

    public async Task<OneOf<UserNotificationDto, ProblemDetail>> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"users/me/notifications/{notificationId}/read",
            null,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<UserNotificationDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "users/me/notifications/read-all",
            null,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
    }

    public string GetStreamUrl()
    {
        var baseUri = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        return $"{baseUri}/users/me/notifications/stream";
    }
}
