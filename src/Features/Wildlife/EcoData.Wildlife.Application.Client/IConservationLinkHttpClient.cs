using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.Application.Client;

public interface IConservationLinkHttpClient
{
    Task<ConservationLinksDtoForSpecies> GetBySpeciesAsync(Guid speciesId, CancellationToken ct = default);
}
