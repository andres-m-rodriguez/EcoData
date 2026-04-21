using EcoData.Common.Pagination;

namespace EcoData.Wildlife.Contracts.Parameters;

public sealed record SpeciesParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? MunicipalityId = null,
    bool? IsFauna = null
) : CursorParameters(PageSize, Cursor);
