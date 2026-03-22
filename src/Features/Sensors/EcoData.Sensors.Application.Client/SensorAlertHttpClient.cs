using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public sealed class SensorAlertHttpClient(HttpClient httpClient) : ISensorAlertHttpClient
{
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
            $"api/sensors/alerts{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<SensorHealthAlertDtoForDetail, ProblemDetail>> GetAlertByIdAsync(
        Guid alertId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"/api/sensors/alerts/{alertId}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return await response.ReadProblemAsync(cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<SensorHealthAlertDtoForDetail>(
            cancellationToken
        );
        return result!;
    }
}
