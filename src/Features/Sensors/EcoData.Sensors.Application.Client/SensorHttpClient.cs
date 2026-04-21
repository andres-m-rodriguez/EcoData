using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public async Task<OneOf<SensorDtoForRegistered, ProblemDetail>> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "sensors/register",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<SensorDtoForRegistered>(
            cancellationToken
        );
        return result!;
    }

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
            .Add("municipalityId", parameters.MunicipalityId)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SensorDtoForList>(
            $"sensors{queryString}",
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
            .Add("municipalityId", parameters.MunicipalityId)
            .Build();

        return httpClient.GetFromJsonAsync<int>(
            $"sensors/count{queryString}",
            cancellationToken
        )!;
    }

    public async Task<SensorDtoForDetail?> GetByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"sensors/{sensorId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(cancellationToken);
    }

    public async Task<OneOf<SensorDtoForDetail, ProblemDetail>> UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"sensors/{sensorId}",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<bool, ProblemDetail>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync($"sensors/{sensorId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        return true;
    }
}
