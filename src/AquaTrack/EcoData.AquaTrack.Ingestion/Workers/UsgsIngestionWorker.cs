using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.Ingestion.Models;
using EcoData.AquaTrack.Ingestion.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.AquaTrack.Ingestion.Workers;

public sealed class UsgsIngestionWorker(
    IServiceProvider serviceProvider,
    IUsgsApiClient usgsApiClient,
    ILogger<UsgsIngestionWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("USGS Ingestion Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IngestDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during USGS data ingestion");
            }

            await Task.Delay(DefaultInterval, stoppingToken);
        }
    }

    private async Task IngestDataAsync(CancellationToken stoppingToken)
    {
        var response = await usgsApiClient.GetInstantaneousValuesAsync(cancellationToken: stoppingToken);

        if (response?.Value.TimeSeries is not { Count: > 0 } timeSeries)
        {
            logger.LogWarning("No time series data received from USGS");
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AquaTrackDbContext>();

        var dataSource = await GetOrCreateUsgsDataSourceAsync(context, stoppingToken);
        var existingSensors = await GetExistingSensorsAsync(context, dataSource.Id, stoppingToken);

        var sensorsToAdd = new List<Sensor>();
        var readingsToAdd = new List<Reading>();
        var now = DateTimeOffset.UtcNow;

        foreach (var series in timeSeries)
        {
            var siteCode = series.SourceInfo.SiteCode.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(siteCode))
            {
                continue;
            }

            if (!existingSensors.TryGetValue(siteCode, out var sensor))
            {
                sensor = CreateSensor(series, dataSource.Id, now);
                sensorsToAdd.Add(sensor);
                existingSensors[siteCode] = sensor;
            }

            var parameterCode = series.Variable.VariableCode.FirstOrDefault()?.Value ?? "UNKNOWN";
            var unitCode = series.Variable.Unit.UnitCode;

            foreach (var valuesSet in series.Values)
            {
                foreach (var reading in valuesSet.Value)
                {
                    if (!double.TryParse(reading.Value, out var value))
                    {
                        continue;
                    }

                    readingsToAdd.Add(new Reading
                    {
                        Id = Guid.CreateVersion7(),
                        SensorId = sensor.Id,
                        Parameter = parameterCode,
                        Value = value,
                        Unit = unitCode,
                        RecordedAt = reading.DateTime,
                        IngestedAt = now,
                    });
                }
            }
        }

        if (sensorsToAdd.Count > 0)
        {
            context.Sensors.AddRange(sensorsToAdd);
            logger.LogInformation("Adding {Count} new sensors", sensorsToAdd.Count);
        }

        if (readingsToAdd.Count > 0)
        {
            // Filter out duplicate readings (same sensor, parameter, recorded_at)
            var uniqueReadings = await FilterDuplicateReadingsAsync(context, readingsToAdd, stoppingToken);

            if (uniqueReadings.Count > 0)
            {
                context.Readings.AddRange(uniqueReadings);
                logger.LogInformation("Adding {Count} new readings", uniqueReadings.Count);
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        logger.LogInformation("USGS data ingestion completed");
    }

    private static async Task<DataSource> GetOrCreateUsgsDataSourceAsync(
        AquaTrackDbContext context,
        CancellationToken stoppingToken)
    {
        var dataSource = await context.DataSources
            .FirstOrDefaultAsync(ds => ds.Name == "USGS Puerto Rico", stoppingToken);

        if (dataSource is not null)
        {
            return dataSource;
        }

        dataSource = new DataSource
        {
            Id = Guid.CreateVersion7(),
            Name = "USGS Puerto Rico",
            Type = DataSourceType.Public,
            BaseUrl = "https://waterservices.usgs.gov/nwis/",
            ApiKey = null,
            PullIntervalSeconds = 900,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.DataSources.Add(dataSource);
        return dataSource;
    }

    private static async Task<Dictionary<string, Sensor>> GetExistingSensorsAsync(
        AquaTrackDbContext context,
        Guid dataSourceId,
        CancellationToken stoppingToken)
    {
        var sensors = await context.Sensors
            .Where(s => s.SourceId == dataSourceId)
            .ToListAsync(stoppingToken);

        return sensors.ToDictionary(s => s.ExternalId);
    }

    private static Sensor CreateSensor(UsgsTimeSeries series, Guid dataSourceId, DateTimeOffset now)
    {
        var siteCode = series.SourceInfo.SiteCode.First().Value;
        var location = series.SourceInfo.GeoLocation.GeogLocation;

        return new Sensor
        {
            Id = Guid.CreateVersion7(),
            SourceId = dataSourceId,
            ExternalId = siteCode,
            Name = series.SourceInfo.SiteName,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Municipality = null,
            IsActive = true,
            CreatedAt = now,
        };
    }

    private static async Task<List<Reading>> FilterDuplicateReadingsAsync(
        AquaTrackDbContext context,
        List<Reading> readings,
        CancellationToken stoppingToken)
    {
        if (readings.Count == 0)
        {
            return readings;
        }

        var sensorIds = readings.Select(r => r.SensorId).Distinct().ToList();
        var minRecordedAt = readings.Min(r => r.RecordedAt);
        var maxRecordedAt = readings.Max(r => r.RecordedAt);

        var existingKeys = await context.Readings
            .Where(r => sensorIds.Contains(r.SensorId) &&
                        r.RecordedAt >= minRecordedAt &&
                        r.RecordedAt <= maxRecordedAt)
            .Select(r => new { r.SensorId, r.Parameter, r.RecordedAt })
            .ToListAsync(stoppingToken);

        var existingKeySet = existingKeys
            .Select(k => (k.SensorId, k.Parameter, k.RecordedAt))
            .ToHashSet();

        return readings
            .Where(r => !existingKeySet.Contains((r.SensorId, r.Parameter, r.RecordedAt)))
            .ToList();
    }
}
