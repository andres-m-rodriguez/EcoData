using EcoData.Common.Pagination;

namespace EcoData.Sensors.Contracts.Parameters;

public sealed record ReadingParameters(
    int PageSize = 50,
    Guid? Cursor = null,
    string? Search = null,
    string? Parameter = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null
) : CursorParameters(PageSize, Cursor);
