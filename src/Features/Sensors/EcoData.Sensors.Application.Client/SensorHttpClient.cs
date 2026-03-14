using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Contracts.Requests;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public async Task<SensorRegistrationResultDto?> RegisterAsync(
        RegisterSensorRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/sensors/register",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorRegistrationResultDto>(
            cancellationToken
        );
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

    public async Task<ReadingBatchResult?> PostReadingAsync(
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

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ReadingBatchResult>(cancellationToken);
    }
}
