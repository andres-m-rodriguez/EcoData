using EcoData.Common.Pagination;

namespace EcoData.AquaTrack.Contracts.Parameters;

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
    bool? IsResolved = null
) : CursorParameters(PageSize, Cursor);
