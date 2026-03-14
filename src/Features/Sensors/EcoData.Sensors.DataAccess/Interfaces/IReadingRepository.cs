using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface IReadingRepository
{
    Task<IReadOnlyList<ReadingDtoForDetail>> GetBySensorAsync(
        Guid sensorId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<ReadingDtoForDetail> GetReadingsAsync(
        Guid sensorId,
        ReadingParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> GetDistinctParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ReadingDtoForList>> GetLatestAsync(
        int limit = 50,
        CancellationToken cancellationToken = default
    );

    Task CreateManyAsync(
        ICollection<ReadingDtoForCreate> dtos,
        CancellationToken cancellationToken = default
    );
}
