using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using OneOf;

namespace EcoData.Locations.Application.Client;

public sealed class MunicipalityHttpClient(HttpClient httpClient) : IMunicipalityHttpClient
{
    public IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters? parameters = null,
        CancellationToken ct = default)
    {
        parameters ??= new MunicipalityParameters();

        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("stateCode", parameters.StateCode)
            .Add("stateId", parameters.StateId)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<MunicipalityDtoForList>(
            $"locations/municipalities{queryString}",
            ct)!;
    }

    public async Task<OneOf<IReadOnlyList<MunicipalityDtoForList>, RequestFailed>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<MunicipalityDtoForList>();

        try
        {
            var idParam = string.Join(",", ids);
            var response = await httpClient.GetAsync(
                $"locations/municipalities/by-ids?ids={Uri.EscapeDataString(idParam)}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var municipalities = await response.Content.ReadFromJsonAsync<IReadOnlyList<MunicipalityDtoForList>>(ct);
            if (municipalities is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<MunicipalityDtoForList>, RequestFailed>.FromT0(municipalities);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"locations/municipalities/{id}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var municipality = await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(ct);
            if (municipality is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return municipality;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
