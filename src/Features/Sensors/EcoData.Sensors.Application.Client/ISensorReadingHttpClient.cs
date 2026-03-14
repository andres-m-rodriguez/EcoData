using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Errors;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorReadingHttpClient
{
    IAsyncEnumerable<ReadingDtoForDetail> GetReadingsAsync(
        Guid sensorId,
        ReadingParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> GetReadingParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<ReadingBatchResult, NotFoundError, ValidationError>> PostReadingsAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    );
}
