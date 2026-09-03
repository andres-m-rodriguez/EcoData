using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public sealed class ConservationLinkHttpClient(HttpClient httpClient) : IConservationLinkHttpClient
{
    public async Task<OneOf<ConservationLinksDtoForSpecies, RequestFailed>> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"wildlife/conservation-links/species/{speciesId}", ct);
        var result = await response.ReadOneOfAsync<ConservationLinksDtoForSpecies>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<PracticeActionDtoForList>, RequestFailed>> GetActionsByPracticeAsync(
        string practiceCode,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/conservation-links/practice/{Uri.EscapeDataString(practiceCode)}/actions",
            ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<PracticeActionDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<ActionPracticeDtoForList>, RequestFailed>> GetPracticesByActionAsync(
        string actionCode,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/conservation-links/action/{Uri.EscapeDataString(actionCode)}/practices",
            ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<ActionPracticeDtoForList>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }

    public async Task<OneOf<IReadOnlyList<SpeciesConservationCodesDto>, RequestFailed>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"wildlife/conservation-links/codes-by-municipality/{municipalityId}",
            ct);
        var result = await response.ReadOneOfAsync<IReadOnlyList<SpeciesConservationCodesDto>>(ct);
        return result.MapT1(problem => RequestFailed.From(problem));
    }
}
