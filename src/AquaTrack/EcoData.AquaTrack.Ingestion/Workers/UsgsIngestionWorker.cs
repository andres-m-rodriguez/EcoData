using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.Ingestion.Models;
using EcoData.AquaTrack.Ingestion.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.AquaTrack.Ingestion.Workers;

public sealed class UsgsIngestionWorker(
    IDataSourceRepository dataSourceRepository,
    ISensorRepository sensorRepository,
    IReadingRepository readingRepository,
    IUsgsApiClient usgsApiClient,
    ILogger<UsgsIngestionWorker> logger
) : BackgroundService
{
    private const string UsgsDataSourceName = "USGS Puerto Rico";
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

        var dataSource = await GetOrCreateUsgsDataSourceAsync(stoppingToken);
        var siteCodesInResponse = timeSeries
            .SelectMany(s => s.SourceInfo.SiteCode)
            .Select(sc => sc.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .ToList();

        var existingSensors = await sensorRepository.GetSensorsByExternalIdsAsync(
            dataSource.Id,
            siteCodesInResponse,
            stoppingToken);

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
            await sensorRepository.CreateManyAsync(sensorsToAdd, stoppingToken);
            logger.LogInformation("Added {Count} new sensors", sensorsToAdd.Count);
        }

        if (readingsToAdd.Count > 0)
        {
            var uniqueReadings = await FilterDuplicateReadingsAsync(readingsToAdd, stoppingToken);

            if (uniqueReadings.Count > 0)
            {
                await readingRepository.CreateManyAsync(uniqueReadings, stoppingToken);
                logger.LogInformation("Added {Count} new readings", uniqueReadings.Count);
            }
        }

        logger.LogInformation("USGS data ingestion completed");
    }

    private async Task<DataSource> GetOrCreateUsgsDataSourceAsync(CancellationToken stoppingToken)
    {
        var dataSource = await dataSourceRepository.GetByNameAsync(UsgsDataSourceName, stoppingToken);

        if (dataSource is not null)
        {
            return dataSource;
        }

        dataSource = new DataSource
        {
            Id = Guid.CreateVersion7(),
            Name = UsgsDataSourceName,
            Type = DataSourceType.Public,
            BaseUrl = "https://waterservices.usgs.gov/nwis/",
            ApiKey = null,
            PullIntervalSeconds = 900,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await dataSourceRepository.CreateAsync(dataSource, stoppingToken);
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

    private async Task<List<Reading>> FilterDuplicateReadingsAsync(
        List<Reading> readings,
        CancellationToken stoppingToken)
    {
        if (readings.Count == 0)
        {
            return readings;
        }

        var sensorIds = readings.Select(r => r.SensorId).Distinct();
        var minRecordedAt = readings.Min(r => r.RecordedAt);
        var maxRecordedAt = readings.Max(r => r.RecordedAt);

        var existingKeySet = await readingRepository.GetExistingKeysAsync(
            sensorIds,
            minRecordedAt,
            maxRecordedAt,
            stoppingToken);

        return readings
            .Where(r => !existingKeySet.Contains((r.SensorId, r.Parameter, r.RecordedAt)))
            .ToList();
    }
}
