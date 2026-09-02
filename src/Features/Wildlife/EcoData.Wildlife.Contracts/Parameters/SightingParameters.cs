using EcoData.Common.Pagination;

namespace EcoData.Wildlife.Contracts.Parameters;

public sealed record SightingParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    SightingStatus? Status = null,
    Guid? SpeciesId = null
) : CursorParameters(PageSize, Cursor);
