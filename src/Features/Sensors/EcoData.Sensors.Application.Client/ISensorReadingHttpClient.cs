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

    Task<OneOf<IReadOnlyList<string>, RequestFailed>> GetReadingParametersAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorReadingStatsDto, RequestFailed>> GetStatsAsync(
        Guid sensorId,
        ReadingStatsParameters? parameters = null,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<ReadingBatchResult, RequestFailed>> PostReadingsAsync(
        Guid sensorId,
        SensorReadingDto reading,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<long, RequestFailed>> GetTotalCountAsync(
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SurfaceWaterSummaryDto, RequestFailed>> GetSurfaceWaterSummaryAsync(
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SurfaceWaterStationDto> GetSurfaceWaterStationsAsync(
        SurfaceWaterStationParameters parameters,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SurfaceWaterStationMarkerDto> GetSurfaceWaterMarkersAsync(
        CancellationToken cancellationToken = default
    );
}
