using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.Database.Models;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISensorHealthRepository
{
    Task<SensorHealthStatusDtoForDetail?> GetStatusByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthStatusDtoForList> GetStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<SensorHealthSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<SensorHealthConfigDtoForDetail?> GetConfigByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<SensorHealthConfigDtoForDetail> UpsertConfigAsync(
        Guid sensorId,
        SensorHealthConfigDtoForCreate config,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsAsync(
        SensorHealthAlertParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task UpdateStatusAsync(
        Guid sensorId,
        SensorHealthStatusType status,
        DateTimeOffset? lastReadingAt,
        string? errorMessage,
        CancellationToken cancellationToken = default
    );

    Task RecordReadingAsync(
        Guid sensorId,
        DateTimeOffset readingTime,
        CancellationToken cancellationToken = default
    );

    Task<SensorHealthAlertDtoForList> CreateAlertAsync(
        Guid sensorId,
        SensorHealthAlertType alertType,
        string message,
        CancellationToken cancellationToken = default
    );

    Task ResolveAlertsAsync(Guid sensorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonitoredSensorHealthStatusDto>> GetMonitoredStatusesWithConfigAsync(
        CancellationToken cancellationToken = default
    );
}
