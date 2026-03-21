using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
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

    Task<OneOf<ReadingBatchResult, ProblemDetail>> PostReadingsAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<ReadingDtoForCreate> SubscribeToReadingsAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
