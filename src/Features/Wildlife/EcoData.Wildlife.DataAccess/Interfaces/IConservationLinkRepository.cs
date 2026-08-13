using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface IConservationLinkRepository
{
    Task<ConservationLinksDtoForSpecies> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PracticeActionDtoForList>> GetActionsByPracticeAsync(
        string practiceCode,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ActionPracticeDtoForList>> GetPracticesByActionAsync(
        string actionCode,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SpeciesConservationCodesDto>> GetCodesByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default
    );
}
