using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;

namespace EcoData.Locations.DataAccess.Interfaces;

public interface IMunicipalityRepository
{
    IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<MunicipalityDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<MunicipalityDtoForDetail?> GetByGeoJsonIdAsync(
        string geoJsonId,
        CancellationToken cancellationToken = default
    );

    Task<MunicipalityDtoForDetail?> GetByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<MunicipalityDtoForGeoJson>> GetGeoJsonByStateCodeAsync(
        string stateCode,
        CancellationToken cancellationToken = default
    );
}
