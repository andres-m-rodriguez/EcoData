namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SurfaceWaterSummaryDto(
    int StationsReporting,
    int TotalStations,
    long Readings7d,
    double? MedianStreamflowCfs,
    double? MeanGageHeightFt,
    double? MeanRainfallInches7d,
    int ActiveAlerts
);

public sealed record SurfaceWaterStationDto(
    int Rank,
    Guid SensorId,
    string Name,
    string ExternalId,
    Guid? MunicipalityId,
    decimal Latitude,
    decimal Longitude,
    double? LatestStreamflowCfs,
    double? LatestGageHeightFt,
    DateTimeOffset? LatestRecordedAt,
    string Status,
    IReadOnlyList<double> SparklineFlow
);

public sealed record SurfaceWaterStationMarkerDto(
    Guid SensorId,
    string Name,
    string ExternalId,
    Guid? MunicipalityId,
    decimal Latitude,
    decimal Longitude,
    double? LatestStreamflowCfs,
    double? LatestGageHeightFt,
    DateTimeOffset? LatestRecordedAt,
    string Status
);
