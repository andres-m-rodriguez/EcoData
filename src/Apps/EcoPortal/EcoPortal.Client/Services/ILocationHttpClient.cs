using System.Text.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using OneOf;

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

    Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetMunicipalityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetMunicipalityByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<JsonDocument, RequestFailed>> GetMunicipalitiesGeoJsonAsync(
        string stateCode,
        CancellationToken cancellationToken = default
    );
}
