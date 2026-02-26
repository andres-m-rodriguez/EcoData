using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public sealed class SensorHttpClient(HttpClient httpClient) : ISensorHttpClient
{
    public IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = BuildQueryString(parameters);
        return httpClient.GetFromJsonAsAsyncEnumerable<SensorDtoForList>(
            $"api/sensors{queryString}",
            cancellationToken
        )!;
    }

    private static string BuildQueryString(SensorParameters parameters)
    {
        var queryParams = new List<string>();

        if (parameters.PageSize != 20)
        {
            queryParams.Add($"pageSize={parameters.PageSize}");
        }

        if (parameters.Cursor.HasValue)
        {
            queryParams.Add($"cursor={parameters.Cursor.Value}");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(parameters.Search)}");
        }

        if (parameters.IsActive.HasValue)
        {
            queryParams.Add($"isActive={parameters.IsActive.Value.ToString().ToLower()}");
        }

        if (parameters.DataSourceId.HasValue)
        {
            queryParams.Add($"dataSourceId={parameters.DataSourceId.Value}");
        }

        return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
    }
}
