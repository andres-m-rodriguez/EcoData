using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public async Task<OneOf<SensorDtoForRegistered, ValidationFailed, RequestFailed>> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "sensors/register",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorDtoForRegistered>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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

    public async Task<OneOf<int, RequestFailed>> GetSensorCountAsync(
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

        try
        {
            var response = await httpClient.GetAsync(
                $"sensors/count{queryString}",
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

    public async Task<OneOf<SensorDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync($"sensors/{sensorId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SensorDtoForDetail, ValidationFailed, RequestFailed>> UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"sensors/{sensorId}",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(
                cancellationToken
            );
            return result!;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<OneOf.Types.Success, RequestFailed>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.DeleteAsync($"sensors/{sensorId}", cancellationToken);

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
