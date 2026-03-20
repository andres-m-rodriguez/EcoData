using System.Text.Json;
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

    Task<MunicipalityDtoForDetail?> GetMunicipalityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<MunicipalityDtoForDetail?> GetMunicipalityByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default
    );

    Task<JsonDocument?> GetMunicipalitiesGeoJsonAsync(
        string stateCode,
        CancellationToken cancellationToken = default
    );
}
