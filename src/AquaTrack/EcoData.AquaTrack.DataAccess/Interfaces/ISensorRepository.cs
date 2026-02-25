using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface ISensorRepository
{
    Task<bool> ExistsAsync(string externalId, Guid dataSourceId, CancellationToken cancellationToken = default);
    Task<SensorDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDtoForList>> GetByDataSourceAsync(Guid dataSourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDtoForList>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, SensorDtoForCreated>> GetSensorsByExternalIdsAsync(Guid dataSourceId, ICollection<string> externalIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDtoForCreated>> CreateManyAsync(ICollection<SensorDtoForCreate> dtos, CancellationToken cancellationToken = default);
}
