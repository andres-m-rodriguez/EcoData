using EcoData.Common.Problems;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface IConservationLinkHttpClient
{
    Task<OneOf<ConservationLinksDtoForSpecies, RequestFailed>> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<PracticeActionDtoForList>, RequestFailed>> GetActionsByPracticeAsync(
        string practiceCode,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<ActionPracticeDtoForList>, RequestFailed>> GetPracticesByActionAsync(
        string actionCode,
        CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<SpeciesConservationCodesDto>, RequestFailed>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken ct = default);
}
