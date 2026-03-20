using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoData.Locations.Application.Client;

public interface IMunicipalityHttpClient
{
    IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters? parameters = null,
        CancellationToken ct = default);

    Task<MunicipalityDtoForDetail?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
