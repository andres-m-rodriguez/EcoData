using EcoData.Common.Problems.Contracts;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using OneOf;

namespace EcoData.Locations.Application.Client;

public interface IMunicipalityHttpClient
{
    IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters? parameters = null,
        CancellationToken ct = default);

    Task<OneOf<MunicipalityDtoForDetail, RequestFailed>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OneOf<IReadOnlyList<MunicipalityDtoForList>, RequestFailed>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<OneOf<string, RequestFailed>> GetGeoJsonByStateCodeAsync(string stateCode, CancellationToken ct = default);
}
