using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISensorRepository
{
    Task<bool> ExistsAsync(
        string externalId,
        Guid dataSourceId,
        CancellationToken cancellationToken = default
    );
    Task<SensorDtoForDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorDtoForList>> GetByDataSourceAsync(
        Guid dataSourceId,
        CancellationToken cancellationToken = default
    );
    IAsyncEnumerable<SensorDtoForList> GetSensorsAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );
    Task<int> GetSensorCountAsync(
        SensorParameters parameters,
        CancellationToken cancellationToken = default
    );
    Task<Dictionary<string, SensorDtoForCreated>> GetSensorsByExternalIdsAsync(
        Guid dataSourceId,
        ICollection<string> externalIds,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<SensorDtoForCreated>> CreateManyAsync(
        Guid organizationId,
        ICollection<SensorDtoForCreate> dtos,
        CancellationToken cancellationToken = default
    );

    Task<SensorDtoForDetail?> UpdateAsync(
        Guid id,
        SensorDtoForUpdate dto,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> GetCountByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorDtoForList> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
