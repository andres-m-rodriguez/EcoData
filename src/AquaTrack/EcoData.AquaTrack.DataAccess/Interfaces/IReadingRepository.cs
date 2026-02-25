using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database.Models;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IReadingRepository
{
    Task<IReadOnlyList<ReadingDto>> GetBySensorAsync(
        Guid sensorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadingWithSensorDto>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<HashSet<(Guid SensorId, string Parameter, DateTimeOffset RecordedAt)>> GetExistingKeysAsync(
        IEnumerable<Guid> sensorIds,
        DateTimeOffset minRecordedAt,
        DateTimeOffset maxRecordedAt,
        CancellationToken cancellationToken = default);

    Task CreateManyAsync(IEnumerable<Reading> readings, CancellationToken cancellationToken = default);
}
