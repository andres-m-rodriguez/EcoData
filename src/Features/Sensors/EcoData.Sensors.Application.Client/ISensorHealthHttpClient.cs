using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorHealthHttpClient
{
    Task<OneOf<SensorHealthSummaryDto, ProblemDetail>> GetSummaryAsync(
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthStatusDtoForList> GetSensorHealthStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthStatusDtoForDetail, ProblemDetail>> GetSensorHealthAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthConfigDtoForDetail, ProblemDetail>> GetSensorHealthConfigAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsAsync(
        SensorHealthAlertParameters parameters,
        CancellationToken cancellationToken = default
    );
}
