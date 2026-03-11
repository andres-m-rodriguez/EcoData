using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.Common.Http.Helpers;

namespace EcoData.AquaTrack.Application.Client;

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
        Guid organizationId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/organizations/{organizationId}/sensors/{sensorId}",
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

    public async Task<SensorDtoForDetail?> UpdateAsync(
        Guid organizationId,
        Guid sensorId,
        SensorDtoForOrganizationCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{organizationId}/sensors/{sensorId}",
            dto,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid organizationId,
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync(
            $"api/organizations/{organizationId}/sensors/{sensorId}",
            cancellationToken
        );

        return response.IsSuccessStatusCode;
    }
}
