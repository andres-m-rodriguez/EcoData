using EcoData.Common.Pagination;

namespace EcoData.AquaTrack.Contracts.Parameters;

public sealed record OrganizationAccessRequestParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Status = null,
    string? Search = null
) : CursorParameters(PageSize, Cursor);
