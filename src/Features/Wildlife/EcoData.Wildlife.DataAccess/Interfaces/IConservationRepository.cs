using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface IConservationRepository
{
    Task<IReadOnlyList<FwsActionDtoForList>> GetAllFwsActionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NrcsPracticeDtoForList>> GetAllNrcsPracticesAsync(CancellationToken cancellationToken = default);

    Task<ConservationLinksDtoForSpecies> GetLinksForSpeciesAsync(
        Guid speciesId,
        CancellationToken cancellationToken = default
    );
}
