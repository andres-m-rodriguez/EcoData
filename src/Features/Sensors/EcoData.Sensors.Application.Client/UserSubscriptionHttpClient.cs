using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class UserSubscriptionHttpClient(HttpClient httpClient) : IUserSubscriptionHttpClient
{
    public async Task<OneOf<IReadOnlyList<UserSensorSubscriptionDto>, RequestFailed>> GetSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("users/me/subscriptions", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<List<UserSensorSubscriptionDto>>(
                cancellationToken
            );
            return OneOf<IReadOnlyList<UserSensorSubscriptionDto>, RequestFailed>.FromT0(result ?? []);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> GetSubscriptionAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"sensors/{sensorId}/subscribe",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> SubscribeAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForCreate request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"sensors/{sensorId}/subscribe",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserSensorSubscriptionDto, RequestFailed>> UpdateSubscriptionAsync(
        Guid sensorId,
        UserSensorSubscriptionDtoForUpdate request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PatchAsJsonAsync(
                $"sensors/{sensorId}/subscribe",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UserSensorSubscriptionDto>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<OneOf.Types.Success, RequestFailed>> UnsubscribeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(
                $"sensors/{sensorId}/subscribe",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            return new OneOf.Types.Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
