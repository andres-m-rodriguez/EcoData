using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
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
}
