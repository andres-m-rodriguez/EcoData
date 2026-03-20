using System.Net.Http.Json;
using System.Text.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoPortal.Client.Services;

public sealed class LocationHttpClient(HttpClient httpClient) : ILocationHttpClient
{
    public IAsyncEnumerable<StateDtoForList> GetStatesAsync(
        StateParameters? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = parameters is null
            ? string.Empty
            : new QueryStringBuilder()
                .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
                .Add("cursor", parameters.Cursor)
                .Add("search", parameters.Search)
                .Build();

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
        var queryString = parameters is null
            ? string.Empty
            : new QueryStringBuilder()
                .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
                .Add("cursor", parameters.Cursor)
                .Add("search", parameters.Search)
                .Add("stateCode", parameters.StateCode)
                .Add("stateId", parameters.StateId)
                .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<MunicipalityDtoForList>(
            $"api/municipalities{queryString}",
            cancellationToken
        )!;
    }

    public async Task<MunicipalityDtoForDetail?> GetMunicipalityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"api/municipalities/{id}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(cancellationToken);
    }

    public async Task<MunicipalityDtoForDetail?> GetMunicipalityByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("latitude", latitude)
            .Add("longitude", longitude)
            .Build();

        var response = await httpClient.GetAsync($"api/municipalities/by-point{queryString}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(cancellationToken);
    }

    public async Task<JsonDocument?> GetMunicipalitiesGeoJsonAsync(
        string stateCode,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/municipalities/geojson/state/{stateCode}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
    }
}
