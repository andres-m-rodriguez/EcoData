using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class UserNotificationHttpClient(HttpClient httpClient) : IUserNotificationHttpClient
{
    public async Task<OneOf<IReadOnlyList<UserNotificationDto>, RequestFailed>> GetNotificationsAsync(
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

        try
        {
            var response = await httpClient.GetAsync(
                $"users/me/notifications{queryString}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<List<UserNotificationDto>>(
                cancellationToken
            );
            return OneOf<IReadOnlyList<UserNotificationDto>, RequestFailed>.FromT0(result ?? []);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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

    public async Task<OneOf<int, RequestFailed>> GetUnreadCountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                "users/me/notifications/unread-count",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UnreadCountDto>(
                cancellationToken
            );
            return result?.Count ?? 0;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserNotificationDto, RequestFailed>> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"users/me/notifications/{notificationId}/read",
                null,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UserNotificationDto>(
                cancellationToken
            );
            if (result is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<int, RequestFailed>> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                "users/me/notifications/read-all",
                null,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
