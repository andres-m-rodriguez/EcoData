using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.Database.Models;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

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

    Task<SensorHealthSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default
    );

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

    Task CreateAlertAsync(
        Guid sensorId,
        SensorHealthAlertType alertType,
        string message,
        CancellationToken cancellationToken = default
    );

    Task ResolveAlertsAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SensorHealthStatus>> GetMonitoredStatusesWithConfigAsync(
        CancellationToken cancellationToken = default
    );
}
