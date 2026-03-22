using System.Runtime.CompilerServices;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class SensorHealthRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISensorHealthRepository
{
    public async Task<SensorHealthStatusDtoForDetail?> GetStatusByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .SensorHealthStatuses.Where(s => s.SensorId == sensorId)
            .Select(s => new SensorHealthStatusDtoForDetail(
                s.Id,
                s.SensorId,
                s.Sensor!.Name,
                s.LastReadingAt,
                s.LastHeartbeatAt,
                s.Status.ToString(),
                s.ConsecutiveFailures,
                s.LastErrorMessage,
                s.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public IAsyncEnumerable<SensorHealthStatusDtoForList> GetStatusesAsync(
        SensorHealthParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetStatusesInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<SensorHealthStatusDtoForList> GetStatusesInternalAsync(
        SensorHealthParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.SensorHealthStatuses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            if (Enum.TryParse<SensorHealthStatusType>(parameters.Status, true, out var status))
            {
                query = query.Where(s => s.Status == status);
            }
        }

        if (parameters.DataSourceId.HasValue)
        {
            query = query.Where(s => s.Sensor!.SourceId == parameters.DataSourceId.Value);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.SensorId > parameters.Cursor.Value);
        }

        await foreach (
            var status in query
                .OrderBy(s => s.SensorId)
                .Take(parameters.PageSize + 1)
                .Select(static s => new SensorHealthStatusDtoForList(
                    s.SensorId,
                    s.Sensor!.Name,
                    s.Status.ToString(),
                    s.LastReadingAt,
                    s.ConsecutiveFailures
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return status;
        }
    }

    public async Task<SensorHealthSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var counts = await context
            .SensorHealthStatuses.GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var healthy =
            counts.FirstOrDefault(c => c.Status == SensorHealthStatusType.Healthy)?.Count ?? 0;
        var stale =
            counts.FirstOrDefault(c => c.Status == SensorHealthStatusType.Stale)?.Count ?? 0;
        var unhealthy =
            counts.FirstOrDefault(c => c.Status == SensorHealthStatusType.Unhealthy)?.Count ?? 0;
        var unknown =
            counts.FirstOrDefault(c => c.Status == SensorHealthStatusType.Unknown)?.Count ?? 0;

        return new SensorHealthSummaryDto(
            TotalMonitored: healthy + stale + unhealthy + unknown,
            Healthy: healthy,
            Stale: stale,
            Unhealthy: unhealthy,
            Unknown: unknown
        );
    }

    public async Task<SensorHealthConfigDtoForDetail?> GetConfigByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context
            .SensorHealthConfigs.Where(c => c.SensorId == sensorId)
            .Select(c => new SensorHealthConfigDtoForDetail(
                c.Id,
                c.SensorId,
                c.ExpectedIntervalSeconds,
                c.StaleThresholdSeconds,
                c.UnhealthyThresholdSeconds,
                c.IsMonitoringEnabled,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SensorHealthConfigDtoForDetail> UpsertConfigAsync(
        Guid sensorId,
        SensorHealthConfigDtoForCreate config,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var existing = await context.SensorHealthConfigs.FirstOrDefaultAsync(
            c => c.SensorId == sensorId,
            cancellationToken
        );

        if (existing is null)
        {
            existing = new SensorHealthConfig
            {
                Id = Guid.CreateVersion7(),
                SensorId = sensorId,
                ExpectedIntervalSeconds = config.ExpectedIntervalSeconds,
                StaleThresholdSeconds = config.StaleThresholdSeconds,
                UnhealthyThresholdSeconds = config.UnhealthyThresholdSeconds,
                IsMonitoringEnabled = config.IsMonitoringEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.SensorHealthConfigs.Add(existing);
        }
        else
        {
            context.SensorHealthConfigs.Attach(existing);
            existing.ExpectedIntervalSeconds = config.ExpectedIntervalSeconds;
            existing.StaleThresholdSeconds = config.StaleThresholdSeconds;
            existing.UnhealthyThresholdSeconds = config.UnhealthyThresholdSeconds;
            existing.IsMonitoringEnabled = config.IsMonitoringEnabled;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new SensorHealthConfigDtoForDetail(
            existing.Id,
            existing.SensorId,
            existing.ExpectedIntervalSeconds,
            existing.StaleThresholdSeconds,
            existing.UnhealthyThresholdSeconds,
            existing.IsMonitoringEnabled,
            existing.CreatedAt,
            existing.UpdatedAt
        );
    }

    public IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsAsync(
        SensorHealthAlertParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetAlertsInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsInternalAsync(
        SensorHealthAlertParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.SensorHealthAlerts.AsQueryable();

        if (parameters.SensorId.HasValue)
        {
            query = query.Where(a => a.SensorId == parameters.SensorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.AlertType))
        {
            if (Enum.TryParse<SensorHealthAlertType>(parameters.AlertType, true, out var alertType))
            {
                query = query.Where(a => a.AlertType == alertType);
            }
        }

        if (parameters.IsResolved.HasValue)
        {
            query = parameters.IsResolved.Value
                ? query.Where(a => a.ResolvedAt != null)
                : query.Where(a => a.ResolvedAt == null);
        }

        if (parameters.FromDate.HasValue)
        {
            query = query.Where(a => a.TriggeredAt >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(a => a.TriggeredAt <= parameters.ToDate.Value);
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(a => a.Id > parameters.Cursor.Value);
        }

        await foreach (
            var alert in query
                .OrderByDescending(a => a.TriggeredAt)
                .Take(parameters.PageSize + 1)
                .Select(static a => new SensorHealthAlertDtoForList(
                    a.Id,
                    a.SensorId,
                    a.Sensor!.Name,
                    a.AlertType.ToString(),
                    a.TriggeredAt,
                    a.ResolvedAt,
                    a.Message
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return alert;
        }
    }

    public async Task UpdateStatusAsync(
        Guid sensorId,
        SensorHealthStatusType status,
        DateTimeOffset? lastReadingAt,
        string? errorMessage,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var existing = await context.SensorHealthStatuses.FirstOrDefaultAsync(
            s => s.SensorId == sensorId,
            cancellationToken
        );

        if (existing is null)
        {
            existing = new SensorHealthStatus
            {
                Id = Guid.CreateVersion7(),
                SensorId = sensorId,
                LastReadingAt = lastReadingAt,
                LastHeartbeatAt = null,
                Status = status,
                ConsecutiveFailures = status == SensorHealthStatusType.Healthy ? 0 : 1,
                LastErrorMessage = errorMessage,
                UpdatedAt = now,
            };
            context.SensorHealthStatuses.Add(existing);
        }
        else
        {
            context.SensorHealthStatuses.Attach(existing);
            existing.Status = status;
            if (lastReadingAt.HasValue)
            {
                existing.LastReadingAt = lastReadingAt;
            }
            existing.LastErrorMessage = errorMessage;
            existing.ConsecutiveFailures =
                status == SensorHealthStatusType.Healthy ? 0 : existing.ConsecutiveFailures + 1;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordReadingAsync(
        Guid sensorId,
        DateTimeOffset readingTime,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var readingTimeUtc = readingTime.ToUniversalTime();

        var existing = await context.SensorHealthStatuses.FirstOrDefaultAsync(
            s => s.SensorId == sensorId,
            cancellationToken
        );

        if (existing is null)
        {
            existing = new SensorHealthStatus
            {
                Id = Guid.CreateVersion7(),
                SensorId = sensorId,
                LastReadingAt = readingTimeUtc,
                LastHeartbeatAt = null,
                Status = SensorHealthStatusType.Healthy,
                ConsecutiveFailures = 0,
                LastErrorMessage = null,
                UpdatedAt = now,
            };
            context.SensorHealthStatuses.Add(existing);
        }
        else
        {
            context.SensorHealthStatuses.Attach(existing);
            existing.LastReadingAt = readingTimeUtc;
            existing.Status = SensorHealthStatusType.Healthy;
            existing.ConsecutiveFailures = 0;
            existing.LastErrorMessage = null;
            existing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SensorHealthAlertDtoForList> CreateAlertAsync(
        Guid sensorId,
        SensorHealthAlertType alertType,
        string message,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var alert = new SensorHealthAlert
        {
            Id = Guid.CreateVersion7(),
            SensorId = sensorId,
            AlertType = alertType,
            TriggeredAt = DateTimeOffset.UtcNow,
            ResolvedAt = null,
            Message = message,
        };

        context.SensorHealthAlerts.Add(alert);
        await context.SaveChangesAsync(cancellationToken);

        var sensorName =
            await context
                .Sensors.Where(s => s.Id == sensorId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

        return new SensorHealthAlertDtoForList(
            alert.Id,
            alert.SensorId,
            sensorName,
            alert.AlertType.ToString(),
            alert.TriggeredAt,
            alert.ResolvedAt,
            alert.Message
        );
    }

    public async Task ResolveAlertsAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var unresolvedAlerts = await context
            .SensorHealthAlerts.Where(a => a.SensorId == sensorId && a.ResolvedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var alert in unresolvedAlerts)
        {
            alert.ResolvedAt = now;
        }

        if (unresolvedAlerts.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<
        IReadOnlyList<MonitoredSensorHealthStatusDto>
    > GetMonitoredStatusesWithConfigAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SensorHealthStatuses.Where(s =>
                s.Sensor!.HealthConfig != null && s.Sensor.HealthConfig.IsMonitoringEnabled
            )
            .Select(s => new MonitoredSensorHealthStatusDto(
                s.Id,
                s.SensorId,
                s.LastReadingAt,
                s.LastHeartbeatAt,
                s.Status.ToString(),
                s.ConsecutiveFailures,
                s.LastErrorMessage,
                s.UpdatedAt,
                s.Sensor!.HealthConfig!.ExpectedIntervalSeconds,
                s.Sensor.HealthConfig.StaleThresholdSeconds,
                s.Sensor.HealthConfig.UnhealthyThresholdSeconds
            ))
            .ToListAsync(cancellationToken);
    }
}
