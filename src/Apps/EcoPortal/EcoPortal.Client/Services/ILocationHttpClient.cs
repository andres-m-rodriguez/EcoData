using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoPortal.Client.Services;

public interface ILocationHttpClient
{
    IAsyncEnumerable<StateDtoForList> GetStatesAsync(
        StateParameters? parameters = null,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters? parameters = null,
        CancellationToken cancellationToken = default
    );
}
