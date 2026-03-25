namespace EcoData.Sensors.Contracts.Dtos;

public sealed record SensorReadingStatsDto(
    IReadOnlyList<ParameterStatsDto> Parameters
);

public sealed record ParameterStatsDto(
    string Parameter,
    string Unit,
    double LatestValue,
    DateTimeOffset LatestReadingAt,
    double Average,
    double Min,
    double Max,
    int Count,
    double? ChangeFromAverage,
    double? ChangeFromPrevious,
    double? Threshold,
    string? ThresholdStatus
);
