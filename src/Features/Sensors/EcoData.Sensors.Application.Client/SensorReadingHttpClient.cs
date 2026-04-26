using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorReadingHttpClient(HttpClient httpClient) : ISensorReadingHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            $"sensors/{sensorId}/readings{queryString}",
            cancellationToken
        )!;
    }

    public async Task<IReadOnlyList<string>> GetReadingParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"sensors/{sensorId}/readings/parameters",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? [];
    }

    public async Task<SensorReadingStatsDto?> GetStatsAsync(
        Guid sensorId,
        ReadingStatsParameters? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("fromDate", parameters?.FromDate)
            .Add("toDate", parameters?.ToDate)
            .Add("parameter", parameters?.Parameter)
            .Build();

        var response = await httpClient.GetAsync(
            $"sensors/{sensorId}/readings/stats{queryString}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SensorReadingStatsDto>(cancellationToken);
    }

    public async Task<OneOf<ReadingBatchResult, ProblemDetail>> PostReadingsAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    )
    {
        var timestamp = reading.RecordedAt ?? DateTimeOffset.UtcNow;
        var readingItems = new List<ReadingItemDto>();

        if (reading.Temperature.HasValue)
            readingItems.Add(new ReadingItemDto("Temperature", null, reading.Temperature.Value, "°C", timestamp));

        if (reading.Ph.HasValue)
            readingItems.Add(new ReadingItemDto("pH", null, reading.Ph.Value, "pH", timestamp));

        if (reading.DissolvedOxygen.HasValue)
            readingItems.Add(new ReadingItemDto("Dissolved Oxygen", null, reading.DissolvedOxygen.Value, "mg/L", timestamp));

        if (reading.Turbidity.HasValue)
            readingItems.Add(new ReadingItemDto("Turbidity", null, reading.Turbidity.Value, "NTU", timestamp));

        if (reading.Conductivity.HasValue)
            readingItems.Add(new ReadingItemDto("Conductivity", null, reading.Conductivity.Value, "µS/cm", timestamp));

        var batch = new ReadingBatchDtoForCreate(sensorId, readingItems);

        var response = await httpClient.PostAsJsonAsync(
            $"sensors/{sensorId}/readings",
            batch,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ReadingBatchResult>(cancellationToken);
            return result!;
        }

        return await response.ReadProblemAsync(cancellationToken);
    }

    public Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return httpClient.GetFromJsonAsync<long>("readings/count", cancellationToken);
    }

    public async Task<SurfaceWaterSummaryDto?> GetSurfaceWaterSummaryAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("readings/topics/surface-water/summary", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SurfaceWaterSummaryDto>(cancellationToken);
    }

    public IAsyncEnumerable<SurfaceWaterStationDto> GetSurfaceWaterStationsAsync(
        SurfaceWaterStationParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 50 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SurfaceWaterStationDto>(
            $"readings/topics/surface-water/stations{queryString}",
            cancellationToken
        )!;
    }

    public IAsyncEnumerable<SurfaceWaterStationMarkerDto> GetSurfaceWaterMarkersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<SurfaceWaterStationMarkerDto>(
            "readings/topics/surface-water/stations/markers",
            cancellationToken
        )!;
    }

    public async IAsyncEnumerable<ReadingDtoForCreate> SubscribeToReadingsAsync(
        Guid sensorId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"sensors/{sensorId}/readings/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var item in SseParser.Create(stream,
            (_, bytes) => JsonSerializer.Deserialize<ReadingDtoForCreate>(bytes, JsonOptions)).EnumerateAsync(cancellationToken))
        {
            if (item.Data is not null)
                yield return item.Data;
        }
    }
}
