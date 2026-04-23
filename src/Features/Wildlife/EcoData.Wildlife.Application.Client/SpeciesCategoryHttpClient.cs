using System.Net.Http.Json;
using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public sealed class SpeciesCategoryHttpClient(HttpClient httpClient) : ISpeciesCategoryHttpClient
{
    public async Task<IReadOnlyList<SpeciesCategoryDtoForList>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("wildlife/species-categories", ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesCategoryDtoForList>>(ct) ?? [];
    }

    public async Task<SpeciesCategoryDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species-categories/{id}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SpeciesCategoryDtoForDetail>(ct);
    }

    public async Task<SpeciesCategoryDtoForDetail?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/species-categories/by-code/{code}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<SpeciesCategoryDtoForDetail>(ct);
    }
}
