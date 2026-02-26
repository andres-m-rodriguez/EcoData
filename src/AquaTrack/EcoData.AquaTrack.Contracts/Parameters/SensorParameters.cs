namespace EcoData.AquaTrack.Contracts.Parameters;

public sealed record SensorParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    bool? IsActive = null,
    Guid? DataSourceId = null
);
