using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHealthHttpClient(HttpClient httpClient) : ISensorHealthHttpClient
{
    public async Task<OneOf<SensorHealthSummaryDto, RequestFailed>> GetSummaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("/sensors/health/summary", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorHealthSummaryDto>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public IAsyncEnumerable<SensorHealthStatusDtoForList> GetSensorHealthStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("status", parameters.Status)
            .Add("dataSourceId", parameters.DataSourceId)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SensorHealthStatusDtoForList>(
            $"sensors/health{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<SensorHealthStatusDtoForDetail, RequestFailed>> GetSensorHealthAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync($"/sensors/{sensorId}/health", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorHealthStatusDtoForDetail>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SensorHealthConfigDtoForDetail, RequestFailed>> GetSensorHealthConfigAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/sensors/{sensorId}/health/config",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorHealthConfigDtoForDetail>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SensorHealthConfigDtoForDetail, RequestFailed>> UpdateHealthConfigAsync(
        Guid sensorId,
        SensorHealthConfigDtoForCreate config,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"/sensors/{sensorId}/health/config",
                config,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorHealthConfigDtoForDetail>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
