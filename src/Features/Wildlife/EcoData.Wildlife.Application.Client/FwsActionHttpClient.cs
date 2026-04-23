using System.Net.Http.Json;
using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public sealed class FwsActionHttpClient(HttpClient httpClient) : IFwsActionHttpClient
{
    public async Task<IReadOnlyList<FwsActionDtoForList>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/fws-actions", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<FwsActionDtoForList>>(ct) ?? [];
    }
}
