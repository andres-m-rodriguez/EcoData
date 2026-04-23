using System.Net.Http.Json;
using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public sealed class ConservationLinkHttpClient(HttpClient httpClient) : IConservationLinkHttpClient
{
    public async Task<ConservationLinksDtoForSpecies> GetBySpeciesAsync(Guid speciesId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/conservation-links/species/{speciesId}", ct);
        if (!response.IsSuccessStatusCode) return new ConservationLinksDtoForSpecies([]);
        return await response.Content.ReadFromJsonAsync<ConservationLinksDtoForSpecies>(ct)
            ?? new ConservationLinksDtoForSpecies([]);
    }
}
