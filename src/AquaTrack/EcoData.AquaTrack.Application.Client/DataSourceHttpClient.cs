using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.Application.Client;

public sealed class DataSourceHttpClient(HttpClient httpClient) : IDataSourceHttpClient
{
    public async Task<IReadOnlyList<DataSourceDtoForList>> GetDataSourcesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await httpClient.GetFromJsonAsync<IReadOnlyList<DataSourceDtoForList>>(
            "api/datasources",
            cancellationToken
        );
        return result ?? [];
    }
}
