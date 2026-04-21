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

public sealed class SensorAlertHttpClient(HttpClient httpClient) : ISensorAlertHttpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsAsync(
        SensorHealthAlertParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("sensorId", parameters.SensorId)
            .Add("alertType", parameters.AlertType)
            .Add("isResolved", parameters.IsResolved)
            .Add("fromDate", parameters.FromDate?.ToString("o"))
            .Add("toDate", parameters.ToDate?.ToString("o"))
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<SensorHealthAlertDtoForList>(
            $"sensors/alerts{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<SensorHealthAlertDtoForDetail, ProblemDetail>> GetAlertByIdAsync(
        Guid alertId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"/sensors/alerts/{alertId}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return await response.ReadProblemAsync(cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<SensorHealthAlertDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async IAsyncEnumerable<SensorHealthAlertDtoForList> SubscribeToAlertsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            "sensors/alerts/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (
            var item in SseParser
                .Create(
                    stream,
                    (_, bytes) =>
                        JsonSerializer.Deserialize<SensorHealthAlertDtoForList>(bytes, JsonOptions)
                )
                .EnumerateAsync(cancellationToken)
        )
        {
            if (item.Data is not null)
                yield return item.Data;
        }
    }

    public async IAsyncEnumerable<SensorHealthAlertDtoForList> SubscribeToSensorAlertsAsync(
        Guid sensorId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var response = await httpClient.GetAsync(
            $"sensors/{sensorId}/alerts/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (
            var item in SseParser
                .Create(
                    stream,
                    (_, bytes) =>
                        JsonSerializer.Deserialize<SensorHealthAlertDtoForList>(bytes, JsonOptions)
                )
                .EnumerateAsync(cancellationToken)
        )
        {
            if (item.Data is not null)
                yield return item.Data;
        }
    }
}
