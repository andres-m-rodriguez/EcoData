using System.Net;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public async Task<
        OneOf<SensorDtoForRegistered, ValidationError, ForbiddenError, ConflictError>
    > RegisterAsync(RegisterSensorRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/sensors/register",
            request,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SensorDtoForRegistered>(
                cancellationToken
            );
            return result!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => new ForbiddenError(
                "You don't have permission to register sensors for this organization"
            ),
            HttpStatusCode.Conflict => new ConflictError(
                "A sensor with this external ID already exists"
            ),
            HttpStatusCode.BadRequest => new ValidationError(
                await response.Content.ReadAsStringAsync(cancellationToken)
            ),
            _ => new ValidationError("Failed to register sensor"),
        };
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
            .Add("municipalityId", parameters.MunicipalityId)
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
        var response = await httpClient.GetAsync($"api/sensors/{sensorId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(cancellationToken);
    }

    public async Task<
        OneOf<SensorDtoForDetail, ValidationError, NotFoundError, ForbiddenError>
    > UpdateAsync(
        Guid sensorId,
        SensorDtoForUpdate request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/sensors/{sensorId}",
            request,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(
                cancellationToken
            );
            return result!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundError(),
            HttpStatusCode.Forbidden => new ForbiddenError(
                "You don't have permission to update this sensor"
            ),
            HttpStatusCode.BadRequest => new ValidationError(
                await response.Content.ReadAsStringAsync(cancellationToken)
            ),
            _ => new ValidationError("Failed to update sensor"),
        };
    }

    public async Task<OneOf<bool, NotFoundError, ForbiddenError>> DeleteAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync($"api/sensors/{sensorId}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundError(),
            HttpStatusCode.Forbidden => new ForbiddenError(
                "You don't have permission to delete this sensor"
            ),
            _ => new NotFoundError(),
        };
    }
}
