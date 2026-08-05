using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHealthHttpClient
{
    Task<OneOf<SensorHealthSummaryDto, RequestFailed>> GetSummaryAsync(
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthStatusDtoForList> GetSensorHealthStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthStatusDtoForDetail, RequestFailed>> GetSensorHealthAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthConfigDtoForDetail, RequestFailed>> GetSensorHealthConfigAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthConfigDtoForDetail, RequestFailed>> UpdateHealthConfigAsync(
        Guid sensorId,
        SensorHealthConfigDtoForCreate config,
        CancellationToken cancellationToken = default
    );
}
