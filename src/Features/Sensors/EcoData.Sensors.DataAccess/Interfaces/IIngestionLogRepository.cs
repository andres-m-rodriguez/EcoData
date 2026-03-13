using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IIngestionLogRepository
{
    Task<IngestionLogDtoForDetail?> GetLatestAsync(Guid dataSourceId, CancellationToken cancellationToken = default);
    Task CreateAsync(IngestionLogDtoForCreate dto, CancellationToken cancellationToken = default);
}
