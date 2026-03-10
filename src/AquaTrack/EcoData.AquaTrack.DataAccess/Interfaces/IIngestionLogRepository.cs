using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IIngestionLogRepository
{
    Task<IngestionLogDtoForDetail?> GetLatestAsync(Guid dataSourceId, CancellationToken cancellationToken = default);
    Task CreateAsync(IngestionLogDtoForCreate dto, CancellationToken cancellationToken = default);
}
