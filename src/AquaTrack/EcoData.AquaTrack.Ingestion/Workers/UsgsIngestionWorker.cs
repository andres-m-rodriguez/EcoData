using EcoData.AquaTrack.Contracts.Dtos;
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

        var sensorsToAdd = new List<SensorDtoForCreate>();
        var pendingSensorIds = new Dictionary<string, Guid>();

        foreach (var series in timeSeries)
        {
            var siteCode = series.SourceInfo.SiteCode.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(siteCode))
            {
                continue;
            }

            if (!existingSensors.ContainsKey(siteCode) && !pendingSensorIds.ContainsKey(siteCode))
            {
                var sensorDto = CreateSensorDto(series, dataSource.Id);
                sensorsToAdd.Add(sensorDto);
                pendingSensorIds[siteCode] = Guid.CreateVersion7();
            }
        }

        if (sensorsToAdd.Count > 0)
        {
            var createdSensors = await sensorRepository.CreateManyAsync(sensorsToAdd, stoppingToken);
            foreach (var sensor in createdSensors)
            {
                existingSensors[sensor.ExternalId] = sensor;
            }
            logger.LogInformation("Added {Count} new sensors", sensorsToAdd.Count);
        }

        var readingsToAdd = new List<ReadingDtoForCreate>();

        foreach (var series in timeSeries)
        {
            var siteCode = series.SourceInfo.SiteCode.FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(siteCode) || !existingSensors.TryGetValue(siteCode, out var sensor))
            {
                continue;
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

                    readingsToAdd.Add(new ReadingDtoForCreate(
                        sensor.Id,
                        parameterCode,
                        value,
                        unitCode,
                        reading.DateTime
                    ));
                }
            }
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

    private async Task<DataSourceDtoForCreated> GetOrCreateUsgsDataSourceAsync(CancellationToken stoppingToken)
    {
        var dataSource = await dataSourceRepository.GetByNameAsync(UsgsDataSourceName, stoppingToken);

        if (dataSource is not null)
        {
            return dataSource;
        }

        var createDto = new DataSourceDtoForCreate(
            UsgsDataSourceName,
            "Public",
            "https://waterservices.usgs.gov/nwis/",
            null,
            900,
            true
        );

        return await dataSourceRepository.CreateAsync(createDto, stoppingToken);
    }

    private static SensorDtoForCreate CreateSensorDto(UsgsTimeSeries series, Guid dataSourceId)
    {
        var siteCode = series.SourceInfo.SiteCode.First().Value;
        var location = series.SourceInfo.GeoLocation.GeogLocation;

        return new SensorDtoForCreate(
            dataSourceId,
            siteCode,
            series.SourceInfo.SiteName,
            location.Latitude,
            location.Longitude,
            null,
            true
        );
    }

    private async Task<List<ReadingDtoForCreate>> FilterDuplicateReadingsAsync(
        List<ReadingDtoForCreate> readings,
        CancellationToken stoppingToken)
    {
        if (readings.Count == 0)
        {
            return readings;
        }

        var sensorIds = readings.Select(r => r.SensorId).Distinct().ToList();
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
