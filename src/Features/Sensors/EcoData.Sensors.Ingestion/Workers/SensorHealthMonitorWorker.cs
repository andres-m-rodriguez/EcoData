using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Database.Models;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.Sensors.Ingestion.Workers;

public sealed class SensorHealthMonitorWorker(
    ISensorHealthRepository healthRepository,
    ILogger<SensorHealthMonitorWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sensor Health Monitor Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSensorHealthAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during sensor health check");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckSensorHealthAsync(CancellationToken cancellationToken)
    {
        var monitoredStatuses = await healthRepository.GetMonitoredStatusesWithConfigAsync(cancellationToken);

        if (monitoredStatuses.Count == 0)
        {
            logger.LogDebug("No sensors with health monitoring enabled");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var staleCount = 0;
        var unhealthyCount = 0;
        var recoveredCount = 0;

        foreach (var status in monitoredStatuses)
        {
            var previousStatus = Enum.Parse<SensorHealthStatusType>(status.Status);
            var newStatus = DetermineHealthStatus(status, now);

            if (newStatus == previousStatus)
            {
                continue;
            }

            await healthRepository.UpdateStatusAsync(
                status.SensorId,
                newStatus,
                status.LastReadingAt,
                GetStatusMessage(newStatus, status.LastReadingAt, now),
                cancellationToken);

            switch (newStatus)
            {
                case SensorHealthStatusType.Stale when previousStatus == SensorHealthStatusType.Healthy:
                    staleCount++;
                    await healthRepository.CreateAlertAsync(
                        status.SensorId,
                        SensorHealthAlertType.Stale,
                        $"Sensor became stale. Last reading: {FormatLastReading(status.LastReadingAt, now)}",
                        cancellationToken);
                    logger.LogWarning("Sensor {SensorId} is now stale. Last reading: {LastReadingAt}",
                        status.SensorId, status.LastReadingAt);
                    break;

                case SensorHealthStatusType.Unhealthy when previousStatus != SensorHealthStatusType.Unhealthy:
                    unhealthyCount++;
                    await healthRepository.CreateAlertAsync(
                        status.SensorId,
                        SensorHealthAlertType.Unhealthy,
                        $"Sensor is unhealthy. Last reading: {FormatLastReading(status.LastReadingAt, now)}",
                        cancellationToken);
                    logger.LogError("Sensor {SensorId} is now unhealthy. Last reading: {LastReadingAt}",
                        status.SensorId, status.LastReadingAt);
                    break;

                case SensorHealthStatusType.Healthy when previousStatus != SensorHealthStatusType.Healthy:
                    recoveredCount++;
                    await healthRepository.ResolveAlertsAsync(status.SensorId, cancellationToken);
                    await healthRepository.CreateAlertAsync(
                        status.SensorId,
                        SensorHealthAlertType.Recovered,
                        "Sensor has recovered and is now healthy",
                        cancellationToken);
                    logger.LogInformation("Sensor {SensorId} has recovered", status.SensorId);
                    break;
            }
        }

        if (staleCount > 0 || unhealthyCount > 0 || recoveredCount > 0)
        {
            logger.LogInformation(
                "Health check completed. Stale: {StaleCount}, Unhealthy: {UnhealthyCount}, Recovered: {RecoveredCount}",
                staleCount, unhealthyCount, recoveredCount);
        }
        else
        {
            logger.LogDebug("Health check completed. No status changes detected");
        }
    }

    private static SensorHealthStatusType DetermineHealthStatus(
        MonitoredSensorHealthStatusDto status,
        DateTimeOffset now)
    {
        if (!status.LastReadingAt.HasValue)
        {
            return SensorHealthStatusType.Unknown;
        }

        var timeSinceLastReading = now - status.LastReadingAt.Value;
        var staleThreshold = TimeSpan.FromSeconds(status.StaleThresholdSeconds);
        var unhealthyThreshold = TimeSpan.FromSeconds(status.UnhealthyThresholdSeconds);

        if (timeSinceLastReading >= unhealthyThreshold)
        {
            return SensorHealthStatusType.Unhealthy;
        }

        if (timeSinceLastReading >= staleThreshold)
        {
            return SensorHealthStatusType.Stale;
        }

        return SensorHealthStatusType.Healthy;
    }

    private static string? GetStatusMessage(
        SensorHealthStatusType status,
        DateTimeOffset? lastReadingAt,
        DateTimeOffset now)
    {
        if (!lastReadingAt.HasValue)
        {
            return "No readings received yet";
        }

        var timeSinceLastReading = now - lastReadingAt.Value;

        return status switch
        {
            SensorHealthStatusType.Stale => $"No data received for {FormatDuration(timeSinceLastReading)}",
            SensorHealthStatusType.Unhealthy => $"No data received for {FormatDuration(timeSinceLastReading)}",
            _ => null
        };
    }

    private static string FormatLastReading(DateTimeOffset? lastReadingAt, DateTimeOffset now)
    {
        if (!lastReadingAt.HasValue)
        {
            return "never";
        }

        var timeSinceLastReading = now - lastReadingAt.Value;
        return $"{FormatDuration(timeSinceLastReading)} ago";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays} day(s)";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours} hour(s)";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes} minute(s)";
        }

        return $"{(int)duration.TotalSeconds} second(s)";
    }
}
