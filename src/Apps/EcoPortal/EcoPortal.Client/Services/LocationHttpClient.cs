using System.Net.Http.Json;
using System.Text.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using OneOf;

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
            $"locations/states{queryString}",
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
            $"locations/municipalities{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetMunicipalityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync($"locations/municipalities/{id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var municipality = await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(cancellationToken);
            if (municipality is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return municipality;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetMunicipalityByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var queryString = new QueryStringBuilder()
                .Add("latitude", latitude)
                .Add("longitude", longitude)
                .Build();

            var response = await httpClient.GetAsync(
                $"locations/municipalities/by-point{queryString}",
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var municipality = await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(cancellationToken);
            if (municipality is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return municipality;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<JsonDocument, RequestFailed>> GetMunicipalitiesGeoJsonAsync(
        string stateCode,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"locations/municipalities/geojson/state/{stateCode}",
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var geoJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            if (geoJson is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return geoJson;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
