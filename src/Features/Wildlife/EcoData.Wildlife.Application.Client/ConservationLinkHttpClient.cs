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

    public async Task<OneOf<IReadOnlyList<PracticeActionDtoForList>, RequestFailed>> GetActionsByPracticeAsync(
        string practiceCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/conservation-links/practice/{Uri.EscapeDataString(practiceCode)}/actions",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var actions = await response.Content.ReadFromJsonAsync<IReadOnlyList<PracticeActionDtoForList>>(ct);
            if (actions is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<PracticeActionDtoForList>, RequestFailed>.FromT0(actions);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<ActionPracticeDtoForList>, RequestFailed>> GetPracticesByActionAsync(
        string actionCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/conservation-links/action/{Uri.EscapeDataString(actionCode)}/practices",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var practices = await response.Content.ReadFromJsonAsync<IReadOnlyList<ActionPracticeDtoForList>>(ct);
            if (practices is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<ActionPracticeDtoForList>, RequestFailed>.FromT0(practices);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<IReadOnlyList<SpeciesConservationCodesDto>, RequestFailed>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"wildlife/conservation-links/codes-by-municipality/{municipalityId}",
                ct);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, ct);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }
            var codes = await response.Content.ReadFromJsonAsync<IReadOnlyList<SpeciesConservationCodesDto>>(ct);
            if (codes is null)
                return new RequestFailed((int)response.StatusCode, "The server returned an empty response.");
            return OneOf<IReadOnlyList<SpeciesConservationCodesDto>, RequestFailed>.FromT0(codes);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
