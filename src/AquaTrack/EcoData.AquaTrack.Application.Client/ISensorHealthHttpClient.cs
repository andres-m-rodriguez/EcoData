using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.AquaTrack.Contracts.Parameters;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface ISensorHealthHttpClient
{
    Task<OneOf<SensorHealthSummaryDto, ApiError>> GetSummaryAsync(
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthStatusDtoForList> GetSensorHealthStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthStatusDtoForDetail, NotFoundError, ApiError>> GetSensorHealthAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthConfigDtoForDetail, NotFoundError, ApiError>> GetSensorHealthConfigAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );
}
