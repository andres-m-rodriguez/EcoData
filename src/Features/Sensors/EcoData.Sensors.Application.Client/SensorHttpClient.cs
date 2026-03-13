using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("isActive", parameters.IsActive)
            .Add("dataSourceId", parameters.DataSourceId)
            .Add("organizationId", parameters.OrganizationId)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SensorDtoForList>(
            $"api/sensors{queryString}",
            cancellationToken
        )!;
    }

    public Task<int> GetSensorCountAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("isActive", parameters.IsActive)
            .Add("dataSourceId", parameters.DataSourceId)
            .Add("organizationId", parameters.OrganizationId)
            .Build();

        return httpClient.GetFromJsonAsync<int>(
            $"api/sensors/count{queryString}",
            cancellationToken
        )!;
    }

    public async Task<SensorDtoForDetail?> GetByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/sensors/{sensorId}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(cancellationToken);
    }

    public async Task<SensorDtoForCreated?> CreateForOrganizationAsync(
        Guid organizationId,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/organizations/{organizationId}/sensors",
            dto,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForCreated>(cancellationToken);
    }
}
