using EcoData.Common.Pagination;

namespace EcoData.Sensors.Contracts.Parameters;

public sealed record NotificationParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? SensorName = null
) : CursorParameters(PageSize, Cursor);
