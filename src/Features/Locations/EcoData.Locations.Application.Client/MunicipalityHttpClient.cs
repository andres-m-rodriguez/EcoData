using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

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
            $"api/municipalities{queryString}",
            ct)!;
    }

    public async Task<MunicipalityDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/municipalities/{id}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MunicipalityDtoForDetail>(ct);
    }
}
