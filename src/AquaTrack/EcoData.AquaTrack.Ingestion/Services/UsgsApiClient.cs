using System.Net.Http.Json;
using EcoData.AquaTrack.Ingestion.Models;
using Microsoft.Extensions.Logging;

namespace EcoData.AquaTrack.Ingestion.Services;

public sealed class UsgsApiClient(
    HttpClient httpClient,
    ILogger<UsgsApiClient> logger
) : IUsgsApiClient
{
    public async Task<UsgsResponse?> GetInstantaneousValuesAsync(
        string stateCode = "PR",
        string period = "P1D",
        CancellationToken cancellationToken = default)
    {
        var url = $"?format=json&stateCd={stateCode}&period={period}&siteStatus=active";

        logger.LogInformation("Fetching USGS data from {Url}", url);

        try
        {
            var response = await httpClient.GetFromJsonAsync<UsgsResponse>(url, cancellationToken);

            if (response?.Value.TimeSeries is { } timeSeries)
            {
                logger.LogInformation("Retrieved {Count} time series from USGS", timeSeries.Count);
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch data from USGS API");
            throw;
        }
    }
}
