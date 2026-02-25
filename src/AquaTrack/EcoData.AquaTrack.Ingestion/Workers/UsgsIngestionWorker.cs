using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
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
                var response = await usgsApiClient.GetInstantaneousValuesAsync(cancellationToken: stoppingToken);

                if (response?.Value.TimeSeries is not { Count: > 0 } timeSeries)
                {
                    logger.LogWarning("No time series data received from USGS");
                    await Task.Delay(DefaultInterval, stoppingToken);
                    continue;
                }

                var dataSource = await dataSourceRepository.GetByNameAsync(UsgsDataSourceName, stoppingToken)
                    ?? await dataSourceRepository.CreateAsync(new DataSourceDtoForCreate(
                        UsgsDataSourceName, "Public", "https://waterservices.usgs.gov/nwis/", null, 900, true
                    ), stoppingToken);

                var siteCodesInResponse = timeSeries
                    .SelectMany(s => s.SourceInfo.SiteCode)
                    .Select(sc => sc.Value)
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .ToList();

                var existingSensors = await sensorRepository.GetSensorsByExternalIdsAsync(
                    dataSource.Id, siteCodesInResponse, stoppingToken);

                var sensorsToAdd = new List<SensorDtoForCreate>();
                var processedSiteCodes = new HashSet<string>();

                foreach (var series in timeSeries)
                {
                    var siteCode = series.SourceInfo.SiteCode.FirstOrDefault()?.Value;
                    if (string.IsNullOrEmpty(siteCode) || existingSensors.ContainsKey(siteCode) || !processedSiteCodes.Add(siteCode))
                    {
                        continue;
                    }

                    var location = series.SourceInfo.GeoLocation.GeogLocation;
                    sensorsToAdd.Add(new SensorDtoForCreate(
                        dataSource.Id, siteCode, series.SourceInfo.SiteName,
                        location.Latitude, location.Longitude, null, true
                    ));
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
                            if (double.TryParse(reading.Value, out var value))
                            {
                                readingsToAdd.Add(new ReadingDtoForCreate(
                                    sensor.Id, parameterCode, value, unitCode, reading.DateTime
                                ));
                            }
                        }
                    }
                }

                if (readingsToAdd.Count > 0)
                {
                    var sensorIds = readingsToAdd.Select(r => r.SensorId).Distinct().ToList();
                    var existingKeys = await readingRepository.GetExistingKeysAsync(
                        sensorIds, readingsToAdd.Min(r => r.RecordedAt), readingsToAdd.Max(r => r.RecordedAt), stoppingToken);

                    var uniqueReadings = readingsToAdd
                        .Where(r => !existingKeys.Contains((r.SensorId, r.Parameter, r.RecordedAt)))
                        .ToList();

                    if (uniqueReadings.Count > 0)
                    {
                        await readingRepository.CreateManyAsync(uniqueReadings, stoppingToken);
                        logger.LogInformation("Added {Count} new readings", uniqueReadings.Count);
                    }
                }

                logger.LogInformation("USGS data ingestion completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during USGS data ingestion");
            }

            await Task.Delay(DefaultInterval, stoppingToken);
        }
    }
}
