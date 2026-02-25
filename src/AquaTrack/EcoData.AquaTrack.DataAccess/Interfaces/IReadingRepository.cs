using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IReadingRepository
{
    Task<IReadOnlyList<ReadingDtoForDetail>> GetBySensorAsync(
        Guid sensorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadingDtoForList>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<HashSet<(Guid SensorId, string Parameter, DateTimeOffset RecordedAt)>> GetExistingKeysAsync(
        ICollection<Guid> sensorIds,
        DateTimeOffset minRecordedAt,
        DateTimeOffset maxRecordedAt,
        CancellationToken cancellationToken = default);

    Task CreateManyAsync(ICollection<ReadingDtoForCreate> dtos, CancellationToken cancellationToken = default);
}
