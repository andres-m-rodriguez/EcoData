using System.Net.Http.Json;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoData.AquaTrack.Application.Client;

public sealed class LocationHttpClient(HttpClient httpClient) : ILocationHttpClient
{
    public IAsyncEnumerable<StateDtoForList> GetStatesAsync(
        StateParameters? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = BuildStateQueryString(parameters);
        return httpClient.GetFromJsonAsAsyncEnumerable<StateDtoForList>(
            $"api/states{queryString}",
            cancellationToken
        )!;
    }

    public IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = BuildMunicipalityQueryString(parameters);
        return httpClient.GetFromJsonAsAsyncEnumerable<MunicipalityDtoForList>(
            $"api/municipalities{queryString}",
            cancellationToken
        )!;
    }

    private static string BuildStateQueryString(StateParameters? parameters)
    {
        if (parameters is null)
        {
            return string.Empty;
        }

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

        return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
    }

    private static string BuildMunicipalityQueryString(MunicipalityParameters? parameters)
    {
        if (parameters is null)
        {
            return string.Empty;
        }

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

        if (!string.IsNullOrWhiteSpace(parameters.StateCode))
        {
            queryParams.Add($"stateCode={Uri.EscapeDataString(parameters.StateCode)}");
        }

        if (parameters.StateId.HasValue)
        {
            queryParams.Add($"stateId={parameters.StateId.Value}");
        }

        return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
    }
}
