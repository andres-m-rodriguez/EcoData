using EcoData.Common.Pagination;

namespace EcoData.Organization.Contracts.Parameters;

public sealed record OrganizationParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null
) : CursorParameters(PageSize, Cursor);
