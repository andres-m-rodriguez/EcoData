using System.Net.Http.Json;
using EcoData.Sensors.Ingestion.Models;
using Microsoft.Extensions.Logging;

namespace EcoData.Sensors.Ingestion.Services;

public sealed class UsgsApiClient(
    HttpClient httpClient,
    ILogger<UsgsApiClient> logger
) : IUsgsApiClient
{
    public async Task<UsgsResponse?> GetInstantaneousValuesAsync(
        string stateCode = "PR",
        DateTimeOffset? startDt = null,
        CancellationToken cancellationToken = default)
    {
        var timeParam = startDt.HasValue
            ? $"startDT={startDt.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}"
            : "period=PT30M";

        var url = $"?format=json&stateCd={stateCode}&{timeParam}&siteStatus=active";

        logger.LogInformation("Fetching USGS data from {Url}", url);

        var response = await httpClient.GetFromJsonAsync<UsgsResponse>(url, cancellationToken);

        if (response?.Value.TimeSeries is { } timeSeries)
        {
            logger.LogInformation("Retrieved {Count} time series from USGS", timeSeries.Count);
        }

        return response;
    }
}
