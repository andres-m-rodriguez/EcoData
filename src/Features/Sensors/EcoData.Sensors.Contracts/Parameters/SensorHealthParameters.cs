using EcoData.Common.Pagination;

namespace EcoData.Sensors.Contracts.Parameters;

public sealed record SensorHealthParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Status = null,
    Guid? DataSourceId = null
) : CursorParameters(PageSize, Cursor);

public sealed record SensorHealthAlertParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    Guid? SensorId = null,
    string? AlertType = null,
    bool? IsResolved = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null
) : CursorParameters(PageSize, Cursor);
