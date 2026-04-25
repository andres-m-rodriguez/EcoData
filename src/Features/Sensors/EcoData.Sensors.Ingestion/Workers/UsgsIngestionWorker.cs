using EcoData.Locations.DataAccess.Interfaces;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.DataAccess.Resolvers;
using EcoData.Sensors.Ingestion.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.Sensors.Ingestion.Workers;

public sealed class UsgsIngestionWorker(
    IOrganizationRepository organizationRepository,
    IDataSourceRepository dataSourceRepository,
    ISensorRepository sensorRepository,
    IReadingRepository readingRepository,
    IIngestionLogRepository ingestionLogRepository,
    ISensorHealthRepository healthRepository,
    IMunicipalityRepository municipalityRepository,
    ParameterResolver parameterResolver,
    IUsgsApiClient usgsApiClient,
    IHostEnvironment hostEnvironment,
    ILogger<UsgsIngestionWorker> logger
) : BackgroundService
{
    private const string UsgsOrganizationName = "USGS";
    private const string UsgsDataSourceName = "USGS Puerto Rico";
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(15);

    // Dev only: cap historical catch-up so a stale local environment can't request
    // a month of USGS data in one shot and trip the HTTP timeout. Prod always
    // catches up from the last recorded timestamp — losing data there is worse
    // than a slow request.
    private static readonly TimeSpan DevMaxLookback = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("USGS Ingestion Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var organization = await organizationRepository.GetByNameAsync(UsgsOrganizationName, stoppingToken)
                    ?? await organizationRepository.CreateAsync(new OrganizationDtoForCreate(UsgsOrganizationName), stoppingToken);

                var dataSource = await dataSourceRepository.GetByNameAsync(UsgsDataSourceName, stoppingToken)
                    ?? await dataSourceRepository.CreateAsync(new DataSourceDtoForCreate(
                        organization.Id, UsgsDataSourceName, "Public", "https://waterservices.usgs.gov/nwis/", null, 900, true
                    ), stoppingToken);

                var lastLog = await ingestionLogRepository.GetLatestAsync(dataSource.Id, stoppingToken);
                var startDt = lastLog?.LastRecordedAt;
                if (hostEnvironment.IsDevelopment() && startDt is { } last
                    && last < DateTimeOffset.UtcNow - DevMaxLookback)
                {
                    logger.LogInformation(
                        "Dev: last recorded {Last:O} is older than {Hours}h cap; falling back to USGS short window",
                        last,
                        DevMaxLookback.TotalHours
                    );
                    startDt = null;
                }

                var response = await usgsApiClient.GetInstantaneousValuesAsync(startDt: startDt, cancellationToken: stoppingToken);

                if (response?.Value.TimeSeries is not { Count: > 0 } timeSeries)
                {
                    logger.LogDebug("No new time series data received from USGS");
                    await Task.Delay(DefaultInterval, stoppingToken);
                    continue;
                }

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
                    var municipality = await municipalityRepository.GetByPointAsync(
                        location.Latitude, location.Longitude, stoppingToken);

                    if (municipality is null)
                    {
                        logger.LogWarning("Skipping sensor {SiteCode} - no municipality found for coordinates ({Lat}, {Lon})",
                            siteCode, location.Latitude, location.Longitude);
                        continue;
                    }

                    sensorsToAdd.Add(new SensorDtoForCreate(
                        dataSource.Id, siteCode, series.SourceInfo.SiteName,
                        location.Latitude, location.Longitude, municipality.Id, true
                    ));
                }

                if (sensorsToAdd.Count > 0)
                {
                    var createdSensors = await sensorRepository.CreateManyAsync(organization.Id, sensorsToAdd, stoppingToken);
                    foreach (var sensor in createdSensors)
                    {
                        existingSensors[sensor.ExternalId] = sensor;
                    }
                    logger.LogInformation("Added {Count} new sensors", sensorsToAdd.Count);
                }

                var parameterLookup = await parameterResolver.LoadLookupAsync(dataSource.Id, stoppingToken);
                var readingsToAdd = new List<ReadingDtoForCreate>();
                var unresolvedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var series in timeSeries)
                {
                    var siteCode = series.SourceInfo.SiteCode.FirstOrDefault()?.Value;
                    if (string.IsNullOrEmpty(siteCode) || !existingSensors.TryGetValue(siteCode, out var sensor))
                    {
                        continue;
                    }

                    var parameterCode = series.Variable.VariableCode.FirstOrDefault()?.Value ?? "UNKNOWN";
                    var variableName = series.Variable.VariableName;
                    var unitCode = series.Variable.Unit.UnitCode;

                    foreach (var valuesSet in series.Values)
                    {
                        foreach (var reading in valuesSet.Value)
                        {
                            if (!double.TryParse(reading.Value, out var value))
                            {
                                continue;
                            }

                            var resolved = parameterLookup.Resolve(parameterCode, unitCode, value, reading.DateTime);
                            if (resolved.PhenomenonId is null)
                            {
                                unresolvedCodes.Add(parameterCode);
                            }

                            readingsToAdd.Add(new ReadingDtoForCreate(
                                sensor.Id,
                                parameterCode,
                                variableName,
                                value,
                                unitCode,
                                reading.DateTime,
                                resolved.PhenomenonId,
                                resolved.ParameterId,
                                resolved.CanonicalValue
                            ));
                        }
                    }
                }

                if (unresolvedCodes.Count > 0)
                {
                    logger.LogWarning(
                        "Stored readings for {Count} unmapped USGS parameter code(s); add Parameter mappings to enable canonical values: {Codes}",
                        unresolvedCodes.Count,
                        string.Join(", ", unresolvedCodes)
                    );
                }

                if (readingsToAdd.Count > 0)
                {
                    await readingRepository.CreateManyAsync(readingsToAdd, stoppingToken);

                    var maxRecordedAt = readingsToAdd.Max(r => r.RecordedAt);
                    await ingestionLogRepository.CreateAsync(
                        new IngestionLogDtoForCreate(dataSource.Id, readingsToAdd.Count, maxRecordedAt),
                        stoppingToken);

                    // Update health status for sensors that received readings
                    var sensorLastReadings = readingsToAdd
                        .GroupBy(r => r.SensorId)
                        .ToDictionary(g => g.Key, g => g.Max(r => r.RecordedAt));

                    foreach (var (sensorId, lastReading) in sensorLastReadings)
                    {
                        await healthRepository.RecordReadingAsync(sensorId, lastReading, stoppingToken);
                    }

                    logger.LogInformation("Ingested {Count} readings, last recorded at {LastRecordedAt}",
                        readingsToAdd.Count, maxRecordedAt);
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
