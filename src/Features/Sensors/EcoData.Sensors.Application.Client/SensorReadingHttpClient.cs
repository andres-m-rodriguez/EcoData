using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorReadingHttpClient(HttpClient httpClient) : ISensorReadingHttpClient
{
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

    public async Task<OneOf<IReadOnlyList<string>, RequestFailed>> GetReadingParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"sensors/{sensorId}/readings/parameters",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(
                cancellationToken
            );
            return OneOf<IReadOnlyList<string>, RequestFailed>.FromT0(result ?? []);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SensorReadingStatsDto, RequestFailed>> GetStatsAsync(
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

        try
        {
            var response = await httpClient.GetAsync(
                $"sensors/{sensorId}/readings/stats{queryString}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorReadingStatsDto>(
                cancellationToken
            );
            if (result is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<ReadingBatchResult, RequestFailed>> PostReadingsAsync(
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

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"sensors/{sensorId}/readings",
                batch,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<ReadingBatchResult>(
                cancellationToken
            );
            if (result is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<long, RequestFailed>> GetTotalCountAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("readings/count", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            return await response.Content.ReadFromJsonAsync<long>(cancellationToken);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<SurfaceWaterSummaryDto, RequestFailed>> GetSurfaceWaterSummaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                "readings/topics/surface-water/summary",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SurfaceWaterSummaryDto>(
                cancellationToken
            );
            if (result is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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
}
