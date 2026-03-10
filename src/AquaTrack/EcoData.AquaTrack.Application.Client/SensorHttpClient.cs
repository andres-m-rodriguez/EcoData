using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = BuildQueryString(parameters);
        return httpClient.GetFromJsonAsAsyncEnumerable<SensorDtoForList>(
            $"api/sensors{queryString}",
            cancellationToken
        )!;
    }

    public IAsyncEnumerable<SensorDtoForList> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<SensorDtoForList>(
            $"api/organizations/{organizationId}/sensors",
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

    private static string BuildQueryString(SensorParameters parameters)
    {
        var queryParams = new List<string>();

        if (parameters.PageSize != 20)
        {
            queryParams.Add($"pageSize={parameters.PageSize}");
        }

        if (parameters.Cursor.HasValue)
        {
            queryParams.Add($"cursor={parameters.Cursor.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(parameters.Search)}");
        }

        if (parameters.IsActive.HasValue)
        {
            queryParams.Add($"isActive={parameters.IsActive.Value.ToString().ToLower()}");
        }

        if (parameters.DataSourceId.HasValue)
        {
            queryParams.Add($"dataSourceId={parameters.DataSourceId.Value}");
        }

        return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
    }
}
