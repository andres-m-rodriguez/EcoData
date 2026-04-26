using EcoData.Common.Pagination;

namespace EcoData.Sensors.Contracts.Parameters;

public sealed record SurfaceWaterStationParameters(int PageSize = 50, Guid? Cursor = null)
    : CursorParameters(PageSize, Cursor);
