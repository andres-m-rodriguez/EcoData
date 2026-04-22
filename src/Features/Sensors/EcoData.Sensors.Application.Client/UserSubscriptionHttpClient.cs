using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class UserSubscriptionHttpClient(HttpClient httpClient) : IUserSubscriptionHttpClient
{
    public async Task<IReadOnlyList<UserSensorSubscriptionDto>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("users/me/subscriptions", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<UserSensorSubscriptionDto>>(
            cancellationToken
        );
        return result ?? [];
    }

    public async Task<UserSensorSubscriptionDto?> GetSubscriptionAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"sensors/{sensorId}/subscribe",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
            cancellationToken
        );
    }

    public async Task<OneOf<UserSensorSubscriptionDto, ProblemDetail>> SubscribeAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"sensors/{sensorId}/subscribe",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<UserSensorSubscriptionDto, ProblemDetail>> UpdateSubscriptionAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"sensors/{sensorId}/subscribe",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<bool, ProblemDetail>> UnsubscribeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync(
            $"sensors/{sensorId}/subscribe",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        return true;
    }
}
