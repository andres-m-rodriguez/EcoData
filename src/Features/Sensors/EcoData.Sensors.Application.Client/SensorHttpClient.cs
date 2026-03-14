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
    public async Task<OneOf<SensorRegistrationResultDto, ValidationError, ForbiddenError, ConflictError>> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/sensors/register",
            request,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SensorRegistrationResultDto>(cancellationToken);
            return result!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => new ForbiddenError("You don't have permission to register sensors for this organization"),
            HttpStatusCode.Conflict => new ConflictError("A sensor with this external ID already exists"),
            HttpStatusCode.BadRequest => new ValidationError(
                await response.Content.ReadAsStringAsync(cancellationToken)),
            _ => new ValidationError("Failed to register sensor")
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
        var response = await httpClient.GetAsync($"api/sensors/{sensorId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorDtoForDetail>(cancellationToken);
    }

    public IAsyncEnumerable<ReadingDtoForDetail> GetReadingsAsync(
        Guid sensorId,
        ReadingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 50 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("parameter", parameters.Parameter)
            .Add("fromDate", parameters.FromDate)
            .Add("toDate", parameters.ToDate)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<ReadingDtoForDetail>(
            $"api/sensors/{sensorId}/readings{queryString}",
            cancellationToken
        )!;
    }

    public async Task<IReadOnlyList<string>> GetReadingParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/sensors/{sensorId}/readings/parameters",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? [];
    }

    public async Task<OneOf<ReadingBatchResult, NotFoundError, ValidationError>> PostReadingAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    )
    {
        var timestamp = reading.RecordedAt ?? DateTimeOffset.UtcNow;
        var readingItems = new List<ReadingItemDto>();

        if (reading.Temperature.HasValue)
            readingItems.Add(
                new ReadingItemDto("Temperature", reading.Temperature.Value, "°C", timestamp)
            );

        if (reading.Ph.HasValue)
            readingItems.Add(new ReadingItemDto("pH", reading.Ph.Value, "pH", timestamp));

        if (reading.DissolvedOxygen.HasValue)
            readingItems.Add(
                new ReadingItemDto(
                    "Dissolved Oxygen",
                    reading.DissolvedOxygen.Value,
                    "mg/L",
                    timestamp
                )
            );

        if (reading.Turbidity.HasValue)
            readingItems.Add(
                new ReadingItemDto("Turbidity", reading.Turbidity.Value, "NTU", timestamp)
            );

        if (reading.Conductivity.HasValue)
            readingItems.Add(
                new ReadingItemDto("Conductivity", reading.Conductivity.Value, "µS/cm", timestamp)
            );

        var batch = new ReadingBatchDtoForCreate(sensorId, readingItems);

        var response = await httpClient.PostAsJsonAsync(
            "/api/push/readings",
            batch,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ReadingBatchResult>(cancellationToken);
            return result!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => new NotFoundError(),
            HttpStatusCode.BadRequest => new ValidationError(
                await response.Content.ReadAsStringAsync(cancellationToken)),
            _ => new ValidationError("Failed to post reading")
        };
    }
}
