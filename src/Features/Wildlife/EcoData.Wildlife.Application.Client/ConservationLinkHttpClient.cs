using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class ConservationLinkHttpClient(HttpClient httpClient) : IConservationLinkHttpClient
{
    public async Task<OneOf<ConservationLinksDtoForSpecies, RequestFailed>> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"wildlife/conservation-links/species/{speciesId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var links = await response.Content.ReadFromJsonAsync<ConservationLinksDtoForSpecies>(ct);
            if (links is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return links;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<IReadOnlyList<SpeciesConservationCodesDto>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/conservation-links/codes-by-municipality/{municipalityId}",
            ct);

        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesConservationCodesDto>>(ct) ?? [];
    }
}
