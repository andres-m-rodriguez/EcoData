namespace EcoData.Sensors.Contracts.Parameters;

public sealed record ReadingStatsParameters(
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? Parameter = null
);
