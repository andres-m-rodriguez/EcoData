using EcoData.Common.Pagination;

namespace EcoData.Identity.Contracts.Parameters;

public sealed record AccessRequestParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    string? Status = null
) : CursorParameters(PageSize, Cursor);
