using System.Net.Http.Json;
using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public sealed class NrcsPracticeHttpClient(HttpClient httpClient) : INrcsPracticeHttpClient
{
    public async Task<IReadOnlyList<NrcsPracticeDtoForList>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/nrcs-practices", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<NrcsPracticeDtoForList>>(ct) ?? [];
    }
}
