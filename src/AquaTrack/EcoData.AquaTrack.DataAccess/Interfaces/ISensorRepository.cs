using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database.Models;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface ISensorRepository
{
    Task<bool> ExistsAsync(string externalId, Guid dataSourceId, CancellationToken cancellationToken = default);
    Task<Sensor?> GetByExternalIdAsync(string externalId, Guid dataSourceId, CancellationToken cancellationToken = default);
    Task<SensorForDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorForListDto>> GetByDataSourceAsync(Guid dataSourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorForListDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, Sensor>> GetSensorsByExternalIdsAsync(Guid dataSourceId, IEnumerable<string> externalIds, CancellationToken cancellationToken = default);
    Task CreateManyAsync(IEnumerable<Sensor> sensors, CancellationToken cancellationToken = default);
}
