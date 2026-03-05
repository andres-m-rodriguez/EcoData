using EcoData.Common.Pagination;

namespace EcoData.Identity.Contracts.Parameters;

public sealed record UserParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null
) : CursorParameters(PageSize, Cursor);
