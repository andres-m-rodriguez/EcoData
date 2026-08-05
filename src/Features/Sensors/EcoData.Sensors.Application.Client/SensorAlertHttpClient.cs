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
            $"sensors/alerts{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<SensorHealthAlertDtoForDetail, RequestFailed>> GetAlertByIdAsync(
        Guid alertId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/sensors/alerts/{alertId}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<SensorHealthAlertDtoForDetail>(
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
}
