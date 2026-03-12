using System.Net;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHealthHttpClient(HttpClient httpClient) : ISensorHealthHttpClient
{
    public async Task<OneOf<SensorHealthSummaryDto, ApiError>> GetSummaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync("/api/health/sensors/summary", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<SensorHealthSummaryDto>(cancellationToken);
        return result!;
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
            $"api/health/sensors{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<SensorHealthStatusDtoForDetail, NotFoundError, ApiError>> GetSensorHealthAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"/api/sensors/{sensorId}/health", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<SensorHealthStatusDtoForDetail>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<SensorHealthConfigDtoForDetail, NotFoundError, ApiError>> GetSensorHealthConfigAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"/api/sensors/{sensorId}/health/config", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<SensorHealthConfigDtoForDetail>(cancellationToken);
        return result!;
    }
}
