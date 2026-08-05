using EcoData.Common.Problems.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using OneOf;

namespace EcoData.Wildlife.Application.Client;

public interface IConservationLinkHttpClient
{
    Task<OneOf<ConservationLinksDtoForSpecies, RequestFailed>> GetBySpeciesAsync(
        Guid speciesId,
        CancellationToken ct = default);
}
